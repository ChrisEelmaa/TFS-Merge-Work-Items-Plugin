using Microsoft.TeamFoundation.Client;
using System;
using System.Net;
namespace Inmeta.TFS.MergeWorkItemsEventHandler
{
	public class NetworkCredentialsProvider : ICredentialsProvider
	{
		private readonly NetworkCredential credentials;
		public NetworkCredentialsProvider(NetworkCredential credentials)
		{
			this.credentials = credentials;
		}
		public ICredentials GetCredentials(Uri uri, ICredentials failedCredentials)
		{
			return this.credentials;
		}
		public void NotifyCredentialsAuthenticated(Uri uri)
		{
		}
	}
}
