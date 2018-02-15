## Nest

Nest is a static site generator that scans a directory for [Markdown][] files and then converts the text to HTML using the [Markdown Sharp][] library. The resulting HTML is then injected into a [Razor][]-based template using [RazorMachine][] to render the final page.

#### Setup

Folder Structure should be:

	/
		/_posts
			(individual .md files for each post - can be grouped into subfolders)
		/_pages
			(individual .md files for each page, including index - can be grouped into subfolders)
		/_templates
			post.cshtml		(template file for individual post)
			page.cshtml		(template file for individual page)
			index.cshtml	(template file for index page)
			(other .cshtml files here)

Everything is output to the root and then to the matching original subfolder.

#### Processing

The following are the steps that are performed during the conversion:

1. scan _posts & _pages (including subfolders) and look for filename collisions
2. parse each individual file & note any overridden template paths
3. generate each file using the template

Templates are passed a SiteData object with the following structure:

	dynamic CurrentItem		//this is the current Post or Page with all properties extracted from the original Markdown file
	List<dynamic> Pages		//this is a collection of all Page items
	List<dynamic> Posts		//this is a collection of all Post items

#### Metadata Properties and Converters:

All Markdown files can contain custom metadata properties in the form of:

	Name: value

The actual content starts after the first blank line. All metadata properties
that are extracted from the Markdown file are provided to the Page/Post item
that is passed to the template. Each Page/Post object also has a `Contains(propertyName)` 
method that can be used to check for the existence of a property before trying to access it. 
If you want the values to be treated as specific data types, you can use the 
`RegisterPropertyConverter()` method (ex: include a 'Date' metadata property, 
use a converter to treat the value as a DateTime object, and use that to sort 
your posts). By leveraging custom metadata along with the complete lists of 
Pages and Posts in templates, complex site structures can be created (ex: posts by category or tag).

In addition to any custom defined metadata propeties, the following properties 
have special meaning. They can be used to override the default values, but if 
not set, they will fall back to their default values.

	OriginalFileName	(the original file name without the extension)
	OriginalFilePath	(the full physical path on disk of the original file)
	OutputFileName		(can be used to explicitly set the name of the generated file; otherwise it will fall back to OriginalFileName.html)
	Template			(can be used to explicitly set the template file path; otherwise will fall back to post.cshtml or page.cshtml)
	Content				(the full generated HTML content of the item)
	Link				(the root-relative link to the file)

(Although any of these can be overridden, it doesn't make a lot of sense to override some of them and may result in unexpected behavior)

#### Usage

	var generator = new Generator(inputPath);
	generator.RegisterPropertyConverter("Date", BasicConverters.DateConverter);
	generator.Generate();


[Markdown]: http://daringfireball.net/projects/markdown/
[Markdown Sharp]: http://code.google.com/p/markdownsharp/
[Razor]: http://msdn.microsoft.com/en-us/vs2010trainingcourse_aspnetmvc3razor.aspx
[RazorMachine]: https://github.com/jlamfers/RazorMachine