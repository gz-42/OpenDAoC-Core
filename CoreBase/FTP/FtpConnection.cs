using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DOL.FTP
{
	/// <summary>
	/// Summary description for FTPConnection.
	/// </summary>
	public class FtpConnection
	{
		private const int BlockSize = 512;
		private const int DataPortRangeFrom = 1500;
		private const int DataPortRangeTo = 65000;
		private const int DefaultRemotePort = 21;
		private int _activeConnectionsCount;

		private bool _logMessages;
		private List<string> _messageList = new List<string>();
		private EFtpMode _mode;
		private string _remoteHost;
		private int _remotePort;
		private TcpClient _tcpClient;
		private readonly Lock _tcpClientLock = new();

		/// <summary>
		/// Creates a new ftp connection
		/// </summary>
		public FtpConnection()
		{
			_activeConnectionsCount = 0;
			_mode = EFtpMode.Active;
			_logMessages = false;
		}

		/// <summary>
		/// The message list containing all the remote messages
		/// </summary>
		public List<string> MessageList
		{
			get { return _messageList; }
		}

		/// <summary>
		/// Sets or gets if messages should be logged
		/// </summary>
		public bool LogMessages
		{
			get { return _logMessages; }

			set
			{
				if (!value)
				{
					_messageList = new List<string>();
				}

				_logMessages = value;
			}
		}

		/// <summary>
		/// Opens a new ftp connection
		/// </summary>
		/// <param name="remoteHost">The remote hostname</param>
		/// <param name="user">The remote user</param>
		/// <param name="password">The remote password</param>
		public virtual void Open(string remoteHost, string user, string password)
		{
			Open(remoteHost, DefaultRemotePort, user, password, EFtpMode.Active);
		}

		/// <summary>
		/// Opens a new ftp connection
		/// </summary>
		/// <param name="remoteHost">The remote hostname</param>
		/// <param name="user">The remote user</param>
		/// <param name="password">The remote password</param>
		/// <param name="mode">The ftp mode</param>
		public virtual void Open(string remoteHost, string user, string password, EFtpMode mode)
		{
			Open(remoteHost, DefaultRemotePort, user, password, mode);
		}

		/// <summary>
		/// Opens a new ftp connection
		/// </summary>
		/// <param name="remoteHost">The remote hostname</param>
		/// <param name="remotePort">The remote port</param>
		/// <param name="user">The remote user</param>
		/// <param name="password">The remote password</param>
		public virtual void Open(string remoteHost, int remotePort, string user, string password)
		{
			Open(remoteHost, remotePort, user, password, EFtpMode.Active);
		}

		/// <summary>
		/// Opens a new ftp connection
		/// </summary>
		/// <param name="remoteHost">The remote hostname</param>
		/// <param name="remotePort">The remote port</param>
		/// <param name="user">The remote user</param>
		/// <param name="password">The remote password</param>
		/// <param name="mode">The ftp mode</param>
		public virtual void Open(string remoteHost, int remotePort, string user, string password, EFtpMode mode)
		{
			var tempMessageList = new List<string>();
			int returnValue;

			_mode = mode;
			_tcpClient = new TcpClient();
			_remoteHost = remoteHost;
			_remotePort = remotePort;

			//CONNECT
			try
			{
				_tcpClient.Connect(_remoteHost, _remotePort);
			}
			catch (Exception)
			{
				throw new IOException("Couldn't connect to remote server");
			}

			tempMessageList = Read();
			returnValue = GetMessageReturnValue(tempMessageList[0]);
			if (returnValue != 220)
			{
				Close();
				throw new Exception(tempMessageList[0]);
			}

			//SEND USER
			tempMessageList = SendCommand("USER " + user);
			returnValue = GetMessageReturnValue(tempMessageList[0]);
			if (!(returnValue == 331 || returnValue == 202))
			{
				Close();
				throw new Exception(tempMessageList[0]);
			}

			//SEND PASSWORD
			if (returnValue == 331)
			{
				tempMessageList = SendCommand("PASS " + password);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (!(returnValue == 230 || returnValue == 202))
				{
					Close();
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		/// <summary>
		/// Closes a connection to the remote server
		/// </summary>
		public virtual void Close()
		{
			if (_tcpClient != null)
			{
				SendCommand("QUIT");
				_tcpClient.Close();
			}
		}

		/// <summary>
		/// Returns a list of remote directories
		/// </summary>
		/// <param name="mask">The mask for the query</param>
		/// <returns>An ArrayList of directories</returns>
		public List<string> GetDirectories(string mask)
		{
			List<string> tmpList = GetDirectories();

			var table = new DataTable();
			table.Columns.Add("Name");
			for (int i = 0; i < tmpList.Count; i++)
			{
				DataRow aRow = table.NewRow();
				aRow["Name"] = tmpList[i];
				table.Rows.Add(aRow);
			}

			DataRow[] aRowList = table.Select("Name LIKE '" + mask + "'", "", DataViewRowState.CurrentRows);
			tmpList = new List<string>();
			for (int i = 0; i < aRowList.Length; i++)
			{
				tmpList.Add((string) aRowList[i]["Name"]);
			}

			return tmpList;
		}

		/// <summary>
		/// Reads the remote directory
		/// </summary>
		/// <returns>An ArrayList with the remote directory contents</returns>
		public List<string> GetDirectories()
		{
			TcpListener listener = null;
			TcpClient client = null;
			NetworkStream networkStream = null;
			List<string> tempMessageList;
			int returnValue = 0;
			string returnValueMessage = "";
			var fileList = new List<string>();

			lock (_tcpClientLock)
			{
				SetTransferType(EFtpFileTransferType.ASCII);

				if (_mode == EFtpMode.Active)
				{
					listener = CreateDataListener();
					listener.Start();
				}
				else
				{
					client = CreateDataClient();
				}

				tempMessageList = SendCommand("NLST");
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (!(returnValue == 150 || returnValue == 125))
				{
					throw new Exception(tempMessageList[0]);
				}

				if (_mode == EFtpMode.Active)
				{
					client = listener.AcceptTcpClient();
				}
				networkStream = client.GetStream();

				fileList = ReadLines(networkStream);

				if (tempMessageList.Count == 1)
				{
					tempMessageList = Read();
					returnValue = GetMessageReturnValue(tempMessageList[0]);
					returnValueMessage = tempMessageList[0];
				}
				else
				{
					returnValue = GetMessageReturnValue(tempMessageList[1]);
					returnValueMessage = tempMessageList[1];
				}

				if (!(returnValue == 226))
				{
					throw new Exception(returnValueMessage);
				}

				networkStream.Close();
				client.Close();

				if (_mode == EFtpMode.Active)
				{
					listener.Stop();
				}
			}

			return fileList;
		}

		/// <summary>
		/// Sends a stream to a remote file
		/// </summary>
		/// <param name="stream">The stream to send</param>
		/// <param name="remoteFileName">The remote file name</param>
		/// <param name="type">The transfer type</param>
		public void SendStream(Stream stream, string remoteFileName, EFtpFileTransferType type)
		{
			TcpListener listener = null;
			TcpClient client = null;
			NetworkStream networkStream = null;
			var tempMessageList = new List<string>();
			int returnValue = 0;
			string returnValueMessage = "";

			lock (_tcpClientLock)
			{
				SetTransferType(type);

				if (_mode == EFtpMode.Active)
				{
					listener = CreateDataListener();
					listener.Start();
				}
				else
				{
					client = CreateDataClient();
				}

				tempMessageList = SendCommand("STOR " + remoteFileName);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (!(returnValue == 150 || returnValue == 125))
				{
					throw new Exception(tempMessageList[0]);
				}

				if (_mode == EFtpMode.Active)
				{
					client = listener.AcceptTcpClient();
				}

				networkStream = client.GetStream();

				var buf = new byte[BlockSize];
				int bytesRead = 0;
				int totalBytes = 0;

				while (totalBytes < stream.Length)
				{
					bytesRead = stream.Read(buf, 0, BlockSize);
					totalBytes = totalBytes + bytesRead;
					networkStream.Write(buf, 0, bytesRead);
				}

				stream.Close();

				networkStream.Close();
				client.Close();

				if (_mode == EFtpMode.Active)
				{
					listener.Stop();
				}

				if (tempMessageList.Count == 1)
				{
					tempMessageList = Read();
					returnValue = GetMessageReturnValue(tempMessageList[0]);
					returnValueMessage = tempMessageList[0];
				}
				else
				{
					returnValue = GetMessageReturnValue(tempMessageList[1]);
					returnValueMessage = tempMessageList[1];
				}

				if (!(returnValue == 226))
				{
					throw new Exception(returnValueMessage);
				}
			}
		}

		/// <summary>
		/// Sends a file to the remote server
		/// </summary>
		/// <param name="localFileName">The local filename</param>
		/// <param name="type">The transfer type</param>
		public virtual void SendFile(string localFileName, EFtpFileTransferType type)
		{
			SendFile(localFileName, Path.GetFileName(localFileName), type);
		}

		/// <summary>
		/// Sends a file to the remote server
		/// </summary>
		/// <param name="localFileName">The local filename</param>
		/// <param name="remoteFileName">The remote filename</param>
		/// <param name="type">The transfer type</param>
		public virtual void SendFile(string localFileName, string remoteFileName, EFtpFileTransferType type)
		{
			using (var file = new FileStream(localFileName, FileMode.Open))
			{
				SendStream(file, remoteFileName, type);
			}
		}

		/// <summary>
		/// Connects a stream to remote file
		/// </summary>
		/// <param name="remoteFileName">The remote file name</param>
		/// <param name="stream">The stream to connect to the remote file</param>
		/// <param name="type">The transfer type</param>
		public void GetStream(string remoteFileName, Stream stream, EFtpFileTransferType type)
		{
			TcpListener listener = null;
			TcpClient client = null;
			NetworkStream networkStream = null;
			List<string> tempMessageList;
			int returnValue = 0;
			string returnValueMessage = "";

			lock (_tcpClientLock)
			{
				SetTransferType(type);

				if (_mode == EFtpMode.Active)
				{
					listener = CreateDataListener();
					listener.Start();
				}
				else
				{
					client = CreateDataClient();
				}

				tempMessageList = SendCommand("RETR " + remoteFileName);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (!(returnValue == 150 || returnValue == 125))
				{
					throw new Exception(tempMessageList[0]);
				}

				if (_mode == EFtpMode.Active)
				{
					client = listener.AcceptTcpClient();
				}

				networkStream = client.GetStream();

				var buffer = new byte[BlockSize];
				int bytesRead = 0;

				bool bRead = true;
				while (bRead)
				{
					bytesRead = networkStream.Read(buffer, 0, buffer.Length);
					stream.Write(buffer, 0, bytesRead);

					if (bytesRead == 0)
					{
						bRead = false;
					}
				}

				stream.Close();

				networkStream.Close();
				client.Close();

				if (_mode == EFtpMode.Active)
				{
					listener.Stop();
				}

				if (tempMessageList.Count == 1)
				{
					tempMessageList = Read();
					returnValue = GetMessageReturnValue(tempMessageList[0]);
					returnValueMessage = tempMessageList[0];
				}
				else
				{
					returnValue = GetMessageReturnValue(tempMessageList[1]);
					returnValueMessage = tempMessageList[1];
				}

				if (!(returnValue == 226))
				{
					throw new Exception(returnValueMessage);
				}
			}
		}

		/// <summary>
		/// Retrieves a remote file
		/// </summary>
		/// <param name="remoteFileName">The remote file name</param>
		/// <param name="type">The transfer type</param>
		public virtual void GetFile(string remoteFileName, EFtpFileTransferType type)
		{
			GetFile(remoteFileName, Path.GetFileName(remoteFileName), type);
		}

		/// <summary>
		/// Retrieves a remote file
		/// </summary>
		/// <param name="remoteFileName">The remote file name</param>
		/// <param name="localFileName">The local file name</param>
		/// <param name="type">The transfer type</param>
		public virtual void GetFile(string remoteFileName, string localFileName, EFtpFileTransferType type)
		{
			GetStream(remoteFileName, new FileStream(localFileName, FileMode.Create), type);
		}

		/// <summary>
		/// Deletes a remote file
		/// </summary>
		/// <param name="remoteFileName">The remote filename</param>
		public virtual void DeleteFile(string remoteFileName)
		{
			lock (_tcpClientLock)
			{
				var tempMessageList = new List<string>();
				int returnValue = 0;

				tempMessageList = SendCommand("DELE " + remoteFileName);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (returnValue != 250)
				{
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		/// <summary>
		/// Sets the remote directory
		/// </summary>
		/// <param name="remotePath">The remote path to set</param>
		public virtual void SetCurrentDirectory(string remotePath)
		{
			var tempMessageList = new List<string>();
			int returnValue = 0;

			lock (_tcpClientLock)
			{
				tempMessageList = SendCommand("CWD " + remotePath);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (returnValue != 250)
				{
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		private void SetTransferType(EFtpFileTransferType type)
		{
			switch (type)
			{
				case EFtpFileTransferType.ASCII:
					SetMode("TYPE A");
					break;
				case EFtpFileTransferType.Binary:
					SetMode("TYPE I");
					break;
				default:
					throw new Exception("Invalid File Transfer Type");
			}
		}

		private void SetMode(string mode)
		{
			var tempMessageList = new List<string>();
			int returnValue = 0;

			lock (_tcpClientLock)
			{
				tempMessageList = SendCommand(mode);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (returnValue != 200)
				{
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		private TcpListener CreateDataListener()
		{
			int port = GetPortNumber();
			SetDataPort(port);
			IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];

			return new TcpListener(ipAddress, port);
		}

		private TcpClient CreateDataClient()
		{
			int port = GetPortNumber();

			var ep = new
				IPEndPoint(GetLocalAddressList()[0], port);

			var client = new TcpClient();

			client.Connect(ep);

			return client;
		}

		private void SetDataPort(int portNumber)
		{
			var tempMessageList = new List<string>();
			int returnValue = 0;
			int iPortHigh = portNumber >> 8;
			int iPortLow = portNumber & 255;

			lock (_tcpClientLock)
			{
				tempMessageList = SendCommand("PORT "
				                              + GetLocalAddressList()[0].ToString().Replace(".", ",")
				                              + "," + iPortHigh + "," + iPortLow);

				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (returnValue != 200)
				{
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		/// <summary>
		/// Creates a remote directory
		/// </summary>
		/// <param name="directoryName">The remote directory to create</param>
		public virtual void CreateDirectory(string directoryName)
		{
			var tempMessageList = new List<string>();
			int returnValue = 0;

			lock (_tcpClientLock)
			{
				tempMessageList = SendCommand("MKD " + directoryName);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (returnValue != 257)
				{
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		/// <summary>
		/// Removes a remote directory
		/// </summary>
		/// <param name="directoryName">The remote directory to remove</param>
		public virtual void RemoveDirectory(string directoryName)
		{
			var tempMessageList = new List<string>();
			int returnValue = 0;

			lock (_tcpClientLock)
			{
				tempMessageList = SendCommand("RMD " + directoryName);
				returnValue = GetMessageReturnValue(tempMessageList[0]);
				if (returnValue != 250)
				{
					throw new Exception(tempMessageList[0]);
				}
			}
		}

		/// <summary>
		/// Sends a specific command to the remote server
		/// </summary>
		/// <param name="command">The command name</param>
		/// <returns>An array containing the response</returns>
		private List<string> SendCommand(string command)
		{
			while (_activeConnectionsCount != 0)
			{
				Thread.Sleep(100);
			}

			_activeConnectionsCount++;

			byte[] cmdBytes = Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());
			NetworkStream aStream = _tcpClient.GetStream();
			aStream.Write(cmdBytes, 0, cmdBytes.Length);

			_activeConnectionsCount--;

			return Read();
		}

		private List<string> Read()
		{
			NetworkStream stream = _tcpClient.GetStream();

			var messageList = new List<string>();

			List<string> tempMessage = ReadLines(stream);
			if (tempMessage.Count > 0)
			{
				while ((tempMessage[tempMessage.Count - 1]).Substring(3, 1) == "-")
				{
					messageList.AddRange(tempMessage);
					tempMessage = ReadLines(stream);
				}
				messageList.AddRange(tempMessage);
			}

			AddMessagesToMessageList(messageList);

			return messageList;
		}

		private static List<string> ReadLines(NetworkStream stream)
		{
			var messageList = new List<string>();
			char[] seperator = {'\n'};
			char[] toRemove = {'\r'};
			var buffer = new byte[BlockSize];
			int bytes = 0;
			string tmpMes = "";
			bool read = true;

			while (read)
			{
				bytes = stream.Read(buffer, 0, buffer.Length);
				tmpMes += Encoding.ASCII.GetString(buffer, 0, bytes);
				if (bytes < buffer.Length)
				{
					read = false;
				}
			}

			string[] mess = tmpMes.Split(seperator);
			for (int i = 0; i < mess.Length; i++)
			{
				if (mess[i].Length > 0)
				{
					messageList.Add(mess[i].Trim(toRemove));
				}
			}

			return messageList;
		}

		private static int GetMessageReturnValue(string message)
		{
			return int.Parse(message.Substring(0, 3));
		}

		private int GetPortNumber()
		{
			int port = 0;

			lock (_tcpClientLock)
			{
				switch (_mode)
				{
					case EFtpMode.Active:
						var rnd = new Random((int) DateTime.Now.Ticks);
						port = DataPortRangeFrom + rnd.Next(DataPortRangeTo - DataPortRangeFrom);
						break;
					case EFtpMode.Passive:
						var tempMessageList = new List<string>();
						int returnValue = 0;

						tempMessageList = SendCommand("PASV");
						returnValue = GetMessageReturnValue(tempMessageList[0]);
						if (returnValue != 227)
						{
							if ((tempMessageList[0]).Length > 4)
							{
								throw new Exception(tempMessageList[0]);
							}
							else
							{
								throw new Exception(tempMessageList[0] + " Passive Mode not implemented");
							}
						}

						string message = tempMessageList[0];
						int index1 = message.IndexOf(",", 0);
						int index2 = message.IndexOf(",", index1 + 1);
						int index3 = message.IndexOf(",", index2 + 1);
						int index4 = message.IndexOf(",", index3 + 1);
						int index5 = message.IndexOf(",", index4 + 1);
						int index6 = message.IndexOf(")", index5 + 1);
						port = 256*int.Parse(message.Substring(index4 + 1, index5 - index4 - 1)) +
						       int.Parse(message.Substring(index5 + 1, index6 - index5 - 1));
						break;
				}
			}

			return port;
		}

		private void AddMessagesToMessageList(List<string> messages)
		{
			if (_logMessages)
			{
				_messageList.AddRange(messages);
			}
		}

		private static IPAddress[] GetLocalAddressList()
		{
			return Dns.GetHostEntry(Dns.GetHostName()).AddressList;
		}
	}
}
