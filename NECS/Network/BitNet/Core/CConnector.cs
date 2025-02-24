using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NECS.Core.Logging;
using System.Threading.Tasks;
using System.Threading;

namespace BitNet
{
	public class CConnector
	{
		public delegate void ConnectedHandler(CUserToken token);
		public ConnectedHandler connected_callback { get; set; }

		Socket client;

		CNetworkService network_service;

		public CConnector(CNetworkService network_service)
		{
			this.network_service = network_service;
			this.connected_callback = null;
		}

		public void connect(IPEndPoint remote_endpoint)
		{
			ConnectAsync(remote_endpoint, 1000)
            .GetAwaiter()
            .GetResult();
		}

		private async Task ConnectAsync(IPEndPoint remoteEndpoint, int timeoutMilliseconds = 5000)
		{
			this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.client.NoDelay = true;

			using (var cts = new CancellationTokenSource(timeoutMilliseconds))
			{
				try
				{
					SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
					eventArgs.Completed += on_connect_completed;
					eventArgs.RemoteEndPoint = remoteEndpoint;

					var tcs = new TaskCompletionSource<bool>();

					eventArgs.UserToken = tcs;
					eventArgs.Completed += (sender, args) =>
					{
						var completionSource = (TaskCompletionSource<bool>)args.UserToken;
						if (args.SocketError == SocketError.Success)
							completionSource.SetResult(true);
						else
							completionSource.SetException(new SocketException((int)args.SocketError));
					};

					bool pending = this.client.ConnectAsync(eventArgs);
					if (!pending)
					{
						if (eventArgs.SocketError != SocketError.Success)
						{
							//throw new SocketException((int)eventArgs.SocketError);
							on_connect_completed(null, eventArgs);
						}
					}

					await Task.WhenAny(tcs.Task, Task.Delay(timeoutMilliseconds, cts.Token));

					if (!tcs.Task.IsCompleted)
					{
						this.client.Close();
						throw new TimeoutException($"Connection to {remoteEndpoint} timed out after {timeoutMilliseconds}ms");
					}

					await tcs.Task;
				}
				catch (Exception ex)
				{
					this.client?.Close();
					throw;
				}
				finally
				{
					cts.Cancel();
				}
			}
		}

		void on_connect_completed(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				//Console.WriteLine("Connect completd!");
				CUserToken token = new CUserToken(this.network_service.logic_entry);

                this.network_service.on_connect_completed(this.client, token);

				if (this.connected_callback != null)
				{
					this.connected_callback(token);
				}
			}
			else
			{
				// failed.
				NLogger.Error(string.Format("Failed to connect. {0}", e.SocketError));
			}
		}
	}
}
