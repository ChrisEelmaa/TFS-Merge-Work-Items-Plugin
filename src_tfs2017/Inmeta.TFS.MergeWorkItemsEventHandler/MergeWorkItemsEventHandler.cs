using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Change = Microsoft.TeamFoundation.VersionControl.Client.Change;
using ChangesetVersionSpec = Microsoft.TeamFoundation.VersionControl.Client.ChangesetVersionSpec;
using MergeSource = Microsoft.TeamFoundation.VersionControl.Client.MergeSource;
using RecursionType = Microsoft.TeamFoundation.VersionControl.Client.RecursionType;

namespace Inmeta.TFS.MergeWorkItemsEventHandler
{
    public class MergeWorkItemsEventHandler : ISubscriber
	{
		public string Name
		{
			get
			{
				return "Inmeta.TFS.MergeWorkItemsEventHandler";
			}
		}
		public SubscriberPriority Priority
		{
			get
			{
				return SubscriberPriority.Normal;
			}
		}
		public Type[] SubscribedTypes()
		{
			return new[]
			{
				typeof(CheckinNotification)
			};
		}
		public EventNotificationStatus ProcessEvent(IVssRequestContext requestContext, NotificationType notificationType, object notificationEventArgs, out int statusCode, out string statusMessage, out ExceptionPropertyCollection properties)
		{
			statusCode = 0;
			properties = null;
			statusMessage = string.Empty;
			try
			{
				if (notificationType == NotificationType.Notification && notificationEventArgs is CheckinNotification)
				{
                    var checkinNotification = notificationEventArgs as CheckinNotification;
					if (ShouldMergeItemsIfNecessary(requestContext, checkinNotification))
					{
						var changeset = requestContext.GetChangeset(checkinNotification.Changeset);
						if (changeset != null)
						{
							var collection = requestContext.GetCollection();
                            MergeWorkItems(collection, changeset.ChangesetId);
						}
					}
				}
			}
			catch (Exception ex)
			{
				TeamFoundationApplicationCore.LogException("Inmeta.TFS.MergeWorkItemEventHandler encountered an exception", ex);
			}

			return EventNotificationStatus.ActionPermitted;
		}
		private static bool ShouldMergeItemsIfNecessary(
			IVssRequestContext requestContext,
			CheckinNotification checkinNotification)
		{
			if (checkinNotification.Comment != null &&
				checkinNotification.Comment.IndexOf("***NO_PBI***", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}

			return true;
		}
		private static void MergeWorkItems(TfsTeamProjectCollection tpc, int changesetId)
		{
			var versionControlServer = (VersionControlServer)tpc.GetService(typeof(VersionControlServer));
			var changesForChangeset = versionControlServer.GetChangesForChangeset(changesetId, false, int.MaxValue, null, null, true);
			var associatedWorkItems = new List<int>();

			foreach (Change pendingMerge in changesForChangeset.PendingMerges())
			{
				foreach (MergeSource mergeSource in pendingMerge.MergeSources)
				{
                    if (sourceAndTargetBranchNeedToMergeWorkItems(pendingMerge, mergeSource))
                    {
                        associatedWorkItems.AddRange(getWorkItemsFromSourceCommits(versionControlServer, mergeSource, associatedWorkItems));
                    }
				}
			}

			AddWorkItemsToChangeset(tpc, changesetId, associatedWorkItems);
		}

        private static IEnumerable<int> getWorkItemsFromSourceCommits(VersionControlServer versionControlServer, MergeSource mergeSource, List<int> associatedWorkItems)
        {
            var mergeHistory = GetMergeHistory(versionControlServer, mergeSource);

            var workItems =
                from Microsoft.TeamFoundation.VersionControl.Client.Changeset cs in mergeHistory
                from wi in
                    from wi in cs.WorkItems
                    where associatedWorkItems.All(w => w != wi.Id)
                    select wi
                select wi;

            return workItems.Select(workItem => workItem.Id);
        }

        private static bool sourceAndTargetBranchNeedToMergeWorkItems(Change pendingMerge, MergeSource mergeSource)
        {
            // merge accross branches
            var sourceItemPath = mergeSource.ServerItem;
            var targetItemPath = pendingMerge.Item.ServerItem;

            var allowedSourceBranchPattern = ConfigurationManager.AppSettings("SourceBranchPattern");
            var allowedTargetBranchPattern = ConfigurationManager.AppSettings("TargetBranchPattern");

            //To mantain the logic of old versions of plugin, if no sourcBranchPattern or targetBranchPattern are informed in config file
            //the old logic will be applied.
            if (string.IsNullOrEmpty(allowedSourceBranchPattern) || string.IsNullOrEmpty(allowedTargetBranchPattern))
            {
                var sourceItemIsInRelease = sourceItemPath.Contains("/Releases");
                var sourceItemIsInTrunk = sourceItemPath.Contains("/Trunk");
                var sourceItemIsInBranches = sourceItemPath.Contains("/Branches");

                var targetItemIsInRelease = targetItemPath.Contains("/Releases");
                var targetItemIsInTrunk = targetItemPath.Contains("/Trunk");
                var targetItemIsInBranches = targetItemPath.Contains("/Branches");

                var mergeWorkItems = (sourceItemIsInBranches || sourceItemIsInTrunk) &&
                                      targetItemIsInRelease;

                if (sourceItemIsInBranches && (targetItemIsInRelease || targetItemIsInTrunk))
                {
                    mergeWorkItems = true;
                }

                if (!mergeWorkItems)
                    return false;
            }
            else
            {
                var sourceItemPathHasSourcePattern = allowedSourceBranchPattern.Split('|').Any(pattern => sourceItemPath.ToLower().Contains(pattern.ToLower()));
                var targetItemPathHasTargetPattern = allowedTargetBranchPattern.Split('|').Any(pattern => targetItemPath.ToLower().Contains(pattern.ToLower()));

                if (!sourceItemPathHasSourcePattern || !targetItemPathHasTargetPattern)
                    return false;
            }

            return true;
        }

        private static IEnumerable GetMergeHistory(VersionControlServer vcs, MergeSource ms)
		{
			var changesetVersionSpec = new ChangesetVersionSpec(ms.VersionFrom);
			var versionTo = new ChangesetVersionSpec(ms.VersionTo);

			return vcs.QueryHistory(
				ms.ServerItem,
				changesetVersionSpec,
				0,
				RecursionType.Full,
				null,
				changesetVersionSpec,
				versionTo,
				2147483647,
				true,
				true);
		}
		private static void AddWorkItemsToChangeset(
			TfsTeamProjectCollection tpc,
			int changeSetId,
			IReadOnlyCollection<int> workItems)
		{
			if (workItems.Count != 0)
			{
				var workItemStore = (WorkItemStore)tpc.GetService(typeof(WorkItemStore));
				var versionControlServer = (VersionControlServer)tpc.GetService(typeof(VersionControlServer));

				foreach (int current in workItems)
				{
					var workItem = workItemStore.GetWorkItem(current);
                    var type = workItemStore.RegisteredLinkTypes["Fixed in Changeset"];

                    var history = "Automatically associated with changeset " + changeSetId;

                    var changeset = versionControlServer.GetChangeset(changeSetId);
					var externalLink = new ExternalLink(type, changeset.ArtifactUri.AbsoluteUri)
					{
						Comment = changeset.Comment
					};
					if (!workItem.Links.ContainsArtifact(externalLink.LinkedArtifactUri))
					{
						workItem.Links.Add(externalLink);
						workItem.History = history;
						workItem.Save();
					}
				}
			}
		}
	}
}
