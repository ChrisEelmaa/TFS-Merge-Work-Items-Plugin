using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using System;
using System.Net;
namespace Inmeta.TFS.MergeWorkItemsEventHandler
{
	public class ImpersonatedCollection
	{
		public static TfsTeamProjectCollection CreateImpersonatedCollection(Uri collectionToUse, string userToImpersonate)
		{
			var defaultNetworkCredentials = CredentialCache.DefaultNetworkCredentials;
            var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionToUse, defaultNetworkCredentials);
            var service = tfsTeamProjectCollection.GetService<IIdentityManagementService>();
            var teamFoundationIdentity = service.ReadIdentity(IdentitySearchFactor.AccountName, userToImpersonate, MembershipQuery.None, ReadIdentityOptions.None);
            return new TfsTeamProjectCollection(collectionToUse, new TfsClientCredentials(), teamFoundationIdentity.Descriptor);
		}
	}
}
