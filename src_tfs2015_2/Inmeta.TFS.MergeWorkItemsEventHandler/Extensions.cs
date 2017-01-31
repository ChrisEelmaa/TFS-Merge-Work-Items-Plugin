using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Services.Location.Server;
using Change = Microsoft.TeamFoundation.VersionControl.Client.Change;
using ChangeType = Microsoft.TeamFoundation.VersionControl.Client.ChangeType;

namespace Inmeta.TFS.MergeWorkItemsEventHandler
{
	public static class Extensions
	{
	    public static TfsTeamProjectCollection GetImpersonatedCollection(this IVssRequestContext requestContext, string userToImpersonate)
		{
			var service = requestContext.GetService<ILocationService>();
			var selfReferenceUri = service.GetSelfReferenceUrl(requestContext, service.GetServerAccessMapping(requestContext));
			return ImpersonatedCollection.CreateImpersonatedCollection(new Uri(selfReferenceUri), userToImpersonate);
		}

		public static IEnumerable<Change> PendingMerges(this Change[] changes)
		{
			return 
				from ch in changes
				where (ch.ChangeType & ChangeType.Merge) == ChangeType.Merge
				select ch;
		}
		public static bool ContainsArtifact(this LinkCollection links, string artifactUri)
		{
			return (
				from l in links.OfType<ExternalLink>()
				select l).Any((ExternalLink el) => el.LinkedArtifactUri == artifactUri);
		}
	}
}
