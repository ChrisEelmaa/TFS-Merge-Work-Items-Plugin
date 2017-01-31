### TFS 2017 Merge Work Items Event Handler
Based on ChrisEelmaa work (https://github.com/ChrisEelmaa/TFS-2015-Merge-Work-Items-Event-Handler) I compiled this handler using TFS 2017 binaries and made some changes. Now, besides the old logic for merge work items (describled below), you have a opportunity to use a simple pattern in configuration file to config what is the __Source Branchs__ and what is the __Target Branchs__. If you have something like:

```
              Hotfix --------------
             /
Main-------------------------------
          \ 
            Test ------------------
             \
               Development --------
```

You can set the configuration file (`assemblyName.dll.config`) as:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="SourceBranchPattern" value="/Develop|/Test|/Hotfix" />
    <add key="TargetBranchPattern" value="/Test|/Main|"/>
  </appSettings>
</configuration>
```


### TFS 2015 Merge Work Items Event Handler (old version)

I have upgraded the existing project ([TFS2013](https://mergeworkitems.codeplex.com)) to work with TFS 2015, and made few minor changes. Few services were deprecated, and had to be replaced. I also changed the logic how the **should work item be merged?** is answered.

Within following logic:

```python
var sourceItemPath = mergeSource.ServerItem;
var targetItemPath = pendingMerge.Item.ServerItem;

var sourceItemIsInRelease = sourceItemPath.Contains("/Releases");
var sourceItemIsInTrunk = sourceItemPath.Contains("/Trunk");
var sourceItemIsInBranches = sourceItemPath.Contains("/Branches");

var targetItemIsInRelease = targetItemPath.Contains("/Releases");
var targetItemIsInTrunk = targetItemPath.Contains("/Trunk");
var targetItemIsInBranches = targetItemPath.Contains("/Branches");

bool mergeWorkItems = (sourceItemIsInBranches || sourceItemIsInTrunk) &&
						targetItemIsInRelease;

if(sourceItemIsInBranches && (targetItemIsInRelease || targetItemIsInTrunk))
{
	mergeWorkItems = true;
}
```

In order to deploy it to your TFS server,
copy everything from /deploy folder/(either tfs_2015 or tfs_2015_update2 folder) to your TFS pugins folder (e to %PROGRAMFILES%\Microsoft Team Foundation Server 14.0\Application Tier\Web Services\bin\Plugins.

That said, keep in mind that this works only for TFS, and not TFS-Git. Also, the above logic might not fit your business needs. 

It is based on the concept that all the release branch paths have /Releases/ in them, and so on. This was necessary to stop the cumulative work item merge between branches. 

For more information, read the author's [post](http://geekswithblogs.net/jakob/archive/2011/05/17/automatically-merging-work-items-in-tfs-2010.aspx)
