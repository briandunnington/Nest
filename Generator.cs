using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkdownSharp;
using Xipton.Razor;
using Xipton.Razor.Config;

namespace Nest
{
    public class Generator
    {
        const string FOLDER_PAGES = "_pages";
        const string FOLDER_POSTS = "_posts";
        const string FOLDER_TEMPLATES = "_templates";
        const string EXT_MARKDOWN = ".md";
        const string EXT_HTML = ".html";
        const string TEMPLATE_PAGE = "page.cshtml";
        const string TEMPLATE_POST = "post.cshtml";

        Dictionary<string, string> specialFolders;
        Dictionary<string, string> templateContents;
        Dictionary<string, Func<string, object>> propertyConverters;
        Markdown markdown;
        RazorMachine razor;

        public Generator(string basePath)
        {
            var di = new DirectoryInfo(basePath);
            BasePath = di.FullName;

            markdown = new Markdown();
            razor = new RazorMachine(htmlEncode:false);

            specialFolders = new Dictionary<string, string>();
            specialFolders.Add(FOLDER_PAGES, null);
            specialFolders.Add(FOLDER_POSTS, null);
            specialFolders.Add(FOLDER_TEMPLATES, null);

            propertyConverters = new Dictionary<string, Func<string, object>>();
        }

        public string BasePath { get; private set; }

        public void RegisterPropertyConverter(string propertyName, Func<string, object> converter)
        {
            propertyConverters[propertyName] = converter;
        }

        public void Generate()
        {
            var pagesFolder = Path.Combine(BasePath, FOLDER_PAGES);
            var postsFolder = Path.Combine(BasePath, FOLDER_POSTS);
            var templatesFolder = Path.Combine(BasePath, FOLDER_TEMPLATES);

            var pageData = new List<dynamic>();
            var postData = new List<dynamic>();

            // Step 1. Loop through all pages and posts (including subfolders) and look for collisions
            var masterFileList = new Dictionary<string, string>();
            var pageList = new Dictionary<string,string>();
            var postList = new Dictionary<string, string>();
            ScanDirectory(masterFileList, pageList, pagesFolder, pagesFolder);
            ScanDirectory(masterFileList, postList, postsFolder, postsFolder);

            // Step 2: Parse Markdown into HTML
            var transformedPages = TransformMarkdownFiles(pageList, Path.Combine(templatesFolder, TEMPLATE_PAGE));
            var transformedPosts = TransformMarkdownFiles(postList, Path.Combine(templatesFolder, TEMPLATE_POST));

            // Step 3: Render HTML templates
            templateContents = new Dictionary<string,string>();
            Render(transformedPages, transformedPages, transformedPosts);
            Render(transformedPosts, transformedPages, transformedPosts);
        }

        void Render(List<dynamic> itemsToRender, List<dynamic> pages, List<dynamic> posts)
        {
            foreach (var item in itemsToRender)
            {
                string templatePath = item.Template;
                if (!templateContents.ContainsKey(templatePath)) templateContents[templatePath] = File.ReadAllText(templatePath);
                var template = templateContents[templatePath];
                var tmpl = razor.ExecuteContent(template, new SiteData() { CurrentItem = item, Pages = pages, Posts = posts });
                var result = tmpl.Result;
                var path = item.OriginalFilePath;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                var outputPath = path + "\\" + item.OutputFileName;
                File.WriteAllText(outputPath, result);
            }
        }

        void ScanDirectory(Dictionary<string, string> masterList, Dictionary<string, string> fileList, string rootPath, string path)
        {
            var di = new DirectoryInfo(path);
            if (rootPath != path && specialFolders.ContainsKey(di.Name)) throw new Exception("Pages and Posts input folders may not contain subfolders with reserved names");

            foreach (var file in di.GetFiles("*" + EXT_MARKDOWN))
            {
                var filePath = file.FullName.Replace(rootPath, String.Empty);
                if (masterList.ContainsKey(filePath)) throw new Exception("Duplicate path: " + filePath);

                masterList.Add(filePath, file.FullName);
                fileList.Add(BasePath + filePath, file.FullName);
            }

            foreach (var folder in di.GetDirectories())
            {
                ScanDirectory(masterList, fileList, rootPath, folder.FullName);
            }
        }

        List<dynamic> TransformMarkdownFiles(Dictionary<string, string> fileList, string defaultTemplate)
        {
            var list = new List<dynamic>();
            foreach (var item in fileList)
            {
                StringBuilder sb = new StringBuilder();
                string[] inputLines = File.ReadAllLines(item.Value);
                bool isReadingMetaData = true;
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                string currentMetadataKey = null;
                foreach (string line in inputLines)
                {
                    if (isReadingMetaData)
                    {
                        if (!String.IsNullOrEmpty(line))
                        {
                            if (line.StartsWith(" ") && currentMetadataKey != null)
                            {
                                metadata[currentMetadataKey] = metadata[currentMetadataKey] + " " + line.Trim();
                                continue;
                            }
                            else
                            {
                                string[] parts = line.Split(':');
                                if (parts.Length >= 2)
                                {
                                    string key = parts[0];
                                    StringBuilder sbv = new StringBuilder();
                                    for (int i = 1; i < parts.Length; i++)
                                    {
                                        sbv.Append(parts[i]);
                                        sbv.Append(" ");
                                    }
                                    string val = sbv.ToString().Trim();
                                    metadata.Add(key, val);
                                    continue;
                                }
                                else
                                {
                                    // this is not really valid, but lets pretend it is
                                    isReadingMetaData = false;
                                }
                            }
                        }
                        else
                        {
                            isReadingMetaData = false;
                        }
                    }

                    sb.AppendLine(line);
                }

                string inputText = sb.ToString();
                string transformedText = markdown.Transform(inputText);

                dynamic p = new System.Dynamic.ExpandoObject();
                var d = (IDictionary<string, object>)p;
                p.Contains = new Func<string, bool>((prop) =>
                {
                    return d.ContainsKey(prop);
                });
                
                foreach (var prop in metadata)
                {
                    if (propertyConverters.ContainsKey(prop.Key))
                        d[prop.Key] = propertyConverters[prop.Key](prop.Value);
                    else
                        d[prop.Key] = prop.Value;
                }

                var originalFileName = Path.GetFileNameWithoutExtension(item.Value);
                var originalFilePath = Path.GetDirectoryName(item.Key);
                var outputFileName = originalFileName + EXT_HTML;
                var relativePath = originalFilePath.Replace(BasePath, String.Empty).Replace("\\", "/");

                d["OriginalFileName"] = originalFileName;
                d["OriginalFilePath"] = originalFilePath;
                d["OutputFileName"] = outputFileName;
                d["Template"] = defaultTemplate;
                d["Content"] = transformedText;
                d["Link"] = relativePath + "/" + outputFileName;

                if(metadata.ContainsKey("OriginalFileName")) d["OriginalFileName"] = metadata["OriginalFileName"];
                if(metadata.ContainsKey("OriginalFilePath")) d["OriginalFilePath"] = metadata["OriginalFilePath"];
                if(metadata.ContainsKey("OutputFileName")) d["OutputFileName"] = metadata["OutputFileName"];
                if(metadata.ContainsKey("Template")) d["Template"] = Path.Combine(BasePath, FOLDER_TEMPLATES, metadata["Template"]);
                if(metadata.ContainsKey("Content")) d["Content"] = metadata["Content"];
                if(metadata.ContainsKey("Link")) d["Link"] = metadata["Link"];

                list.Add(p);
            }
            return list;
        }



        class FileData
        {
            public string FilePath { get; set; }
            public dynamic Data { get; set; }
        }
    }
}
