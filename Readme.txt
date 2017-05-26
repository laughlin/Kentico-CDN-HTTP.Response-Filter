*****************************************
Kentico CDN HTTP.Response Filter
*****************************************

Version 1.0 - May 12, 2017


*****************************************
Purpose
*****************************************

This filter can be used to alter the Kentico web application output HTML responses in order to change the HTML attribute paths to use a Content Delivery Network (CDN) without having to change content manually or modify any /Admin settings.

For example, the following image tags can be modified:

		<img src="/images/file1.jpg"/> can be rewritten:

		<img src="https://2345j2.cloudfront.net/images/file1.jpg"/>

The paths this filter can alter are determined using XPath, which is configured in the web.config AppSettings.

This change does not synchronize your files with your CDN, it merely alters the response paths out of Kentico.

This filter does not require any changes to your IIS server.

An optional Custom Tranformation Method [CdnPrefix()] is also included if you would like to be able to access the CDN logic within the Ketnico Admin.



*****************************************
Setup/Installation
*****************************************

In order to enable this section, merge the attached web.config's file to your own web.config, along with copying the App_Code/CDN/ folder to your CMS/App_Code/ directory.

This filter also relies upon Html Agility Pack (https://htmlagilitypack.codeplex.com/) for more complex parsing that Regex would allow for.
Tested using Html Agility Pack v1.4.9.5.

You can install a couple of ways:

		* Via NuGet package (https://www.nuget.org/packages/HtmlAgilityPack/1.4.9.5)
			A packages.config is supplied if you would prefer to merge the reference.
			
		* Via referencing the following DDL:  HtmlAgilityPack.dll
			and copying the following files to the CMS/bin/ directory:
					HtmlAgilityPack.dll
					HtmlAgilityPack.pdb
					HtmlAgilityPack.xml
					
To install the optional Custom Tranformation Method [CdnPrefix()] copy the App_Code/CustomTransformationMethods/ folder to your CMS/App_Code/ directory (merging if necessary).
Here is an example of how it can be used within a Transformation:
{'teaserUrl':'<%# Eval("BlogPostTeaser").ToString() != "" ? CdnPrefix(ResolveUrl("~/CMSPages/GetFile.aspx?guid=" + Eval("BlogPostTeaser").ToString())) : "" %>'}



*****************************************
Configuration
*****************************************

The following AppSettings can be configured in the web.config:

		CdnEnabled: If set to true, CDN HTTP.Response Filter will be used, if set to false, the filter will not be applied.

		CdnSmallDomain: The domain name to use for the CDN. If using a small and large, use the small for serving assets that are 300KB or less

		CdnLargeDomain: The domain name to use for the large CDN. Only used if serving larger assets from a separate URL

		CdnForceSsl: If set to true, https:// will always be used. Otherwise, it will only use SSL if the current request uses HTTPS.

		CdnXPathHtmlToReplace: XPath string containing selectors for the HTML elements and/or attributes you want to be rewritten. (https://msdn.microsoft.com/en-us/library/ms256086(v=vs.110).aspx)

		CdnFilePathsToIgnore: Comma-delimited paths that should be ignored by the injector. Any file references that contain these strings will not be rewritten. This takes precedence over CdnFilePathsToMatch. NOTE: If the page request path includes the string "/cms", files will never be re-written. This will ensure that Admin content management won't be affected.

		CdnFilePathsToMatch: Comma-delimited paths to be rewritten by the injector. Any file references that contain these strings will be re-written.

		CdnSmallFileExtensionsToMatch: only these file extensions will be re-written using the CdnSmallDomain value

		CdnLargeFileExtensionsToMatch: only these file extensions will be re-written using the CdnLargeDomain value

		CdnDisallowedUrls: Comma-delimited URL paths that should be ignored by the filter. Any URLs accessed that contain these paths will not be rewritten by the filter. 


