using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Text;
using System.Threading;

namespace CPTTCPmobile
{
	[Activity (Label = "CPT-TCP-mobile", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		Button btnConnect;
		Button btnEncrypt;
		Button btnSend;
		EditText txtIP;
		EditText txtMessages;
		EditText txtSend;
		EditText txtLocalIP;

		private Cryptography crypto;

		const int port = 8100;
		string recieverIP = "";
		UdpClient udpServer;

		public static string remotePublicKey = "";
		public static bool keySendt = false;
		public static bool encryptedMode = false;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			btnConnect = FindViewById<Button>(Resource.Id.button1);
			btnEncrypt = FindViewById<Button>(Resource.Id.button2);
			btnSend = FindViewById<Button>(Resource.Id.button3);
			txtIP = FindViewById<EditText>(Resource.Id.editText1);
			txtMessages = FindViewById<EditText>(Resource.Id.editText2);
			txtSend = FindViewById<EditText>(Resource.Id.editText3);
			txtLocalIP = FindViewById<EditText> (Resource.Id.editText4);

			btnConnect.Click += (object sender, EventArgs e) =>
			{
				if(btnConnect.Text == "Connect")
				{
					btnConnect.Text = "Close";
					try
					{
						udpServer.Connect(new IPEndPoint(IPAddress.Parse(txtIP.Text),8100));
						recieverIP = txtIP.Text;
					}
					catch(Exception ex)
					{
						txtMessages.Append("\n" + ex.Message);
					}
				}
				else if(btnConnect.Text == "Close")
				{
					System.Environment.Exit(0);
				}
			};

			btnEncrypt.Click += (object sender, EventArgs e) =>
			{
				// Start encrypted mode on connect
				string wrappedKey = "#publicKeyStarts#" + crypto.publicKey + "#publicKeyStops#";
				udpSend(wrappedKey);
				txtMessages.Append("\n[sent public key]\n\t");
				txtMessages.SetSelection(txtMessages.Length());
				keySendt = true;
			};
			btnSend.Click += (object sender, EventArgs e) =>
			{
				try
				{
					if (remotePublicKey == "")
					{
						txtMessages.Append("\nYou: [uncrypted]\n\t" + txtSend.Text);
						txtMessages.SetSelection(txtMessages.Length());
						// send message unencrypted
						udpSend(txtSend.Text);
						txtSend.Text = "";
					}
					else
					{
						txtMessages.Append("\nYou: [crypted]\n\t" + txtSend.Text);
						txtMessages.SetSelection(txtMessages.Length());
						// Send message encrypted
						string sendmsg = crypto.EncryptMessage(remotePublicKey, txtSend.Text);
						udpSend(sendmsg);
						txtSend.Text = "";
					}
				}
				catch(Exception ex)
				{
					txtMessages.Append("\n" + ex.Message);
				}
			};
			txtSend.KeyPress += (object sender, View.KeyEventArgs e) =>
			{
				if ((e.Event.Action == KeyEventActions.Down) && (e.KeyCode == Keycode.Enter))
				{
					//enter = send message
					try
					{
						if (remotePublicKey == "")
						{
							txtMessages.Append("\nYou: [uncrypted]\n\t" + txtSend.Text);
							txtMessages.SetSelection(txtMessages.Length());
							// send message unencrypted
							udpSend(txtSend.Text);
							txtSend.Text = "";
						}
						else
						{
							txtMessages.Append("\nYou: [crypted]\n\t" + txtSend.Text);
							txtMessages.SetSelection(txtMessages.Length());
							// Send message encrypted
							string sendmsg = crypto.EncryptMessage(remotePublicKey, txtSend.Text);
							udpSend(sendmsg);
							txtSend.Text = "";
						}
					}
					catch(Exception ex)
					{
						txtMessages.Append("\n" + ex.Message);
					}
				}
			};

			txtIP.KeyPress += (object sender, View.KeyEventArgs e) =>
			{
				if ((e.Event.Action == KeyEventActions.Down) && (e.KeyCode == Keycode.Enter))
				{
					//enter = send message
					if(btnConnect.Text == "Connect")
					{
						btnConnect.Text = "Close";
						try
						{
							udpServer.Connect(new IPEndPoint(IPAddress.Parse(txtIP.Text),8100));
							recieverIP = txtIP.Text;
						}
						catch(Exception ex)
						{
							txtMessages.Append("\n" + ex.Message);
						}
					}
				}
			};

			try
			{
				// init the crypto object
				udpServer = new UdpClient(8100);
				crypto = new Cryptography();
				Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
				thread.Start();

				Thread threadGetIP = new Thread(new ThreadStart(getIPfunction));
				threadGetIP.Start();

			}
			catch (Exception ex)
			{
				txtMessages.Append ("\n" + ex.Message);
			}
		}

			// on device found code
		public void udpSend(string text)
		{
			byte[] data = Encoding.ASCII.GetBytes(text);
			udpServer.SendAsync(data, data.Length);
		}

		public void getIPfunction()
		{
			string ip = Toolbox.getIP_External();
			RunOnUiThread (() =>  txtLocalIP.Text = ip);
		}
		public void WorkThreadFunction()
		{
			try
			{
				string recievedText = "";
				while (true)
				{
					//IPEndPoint object will allow us to read datagrams sent from any source.
					var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
					var receivedResults = udpServer.Receive(ref remoteEndPoint);
					recievedText = Encoding.ASCII.GetString(receivedResults);

					//Update remote ip
					if(recieverIP == "") udpServer.Connect(remoteEndPoint);
					RunOnUiThread (() => txtIP.Text = remoteEndPoint.Address.ToString());
					recieverIP = remoteEndPoint.Address.ToString();

					
					if(recieverIP == "") 
					{
						recieverIP = remoteEndPoint.Address.ToString(); // Update the reciever IP if new connection
						RunOnUiThread (() => btnConnect.Text = "Close");
						RunOnUiThread (() => txtIP.Text = recieverIP);
					}

					// Check for public key in the message
					if (recievedText.Contains("#publicKeyStarts#"))
					{
						remotePublicKey = Toolbox.GetStringBetweenStrings(recievedText, "#publicKeyStarts#", "#publicKeyStops#");
						RunOnUiThread (() => txtMessages.Append("\n[recieved public key]\n\t"));
						RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length())); // scroll to end
						if(!keySendt)
						{
							// send key if not sent
							string wrappedKey = "#publicKeyStarts#" + crypto.publicKey + "#publicKeyStops#";
							udpSend(wrappedKey);
							RunOnUiThread (() => txtMessages.Append("\n[sent public key]\n\t"));
							RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length()));
							keySendt = true;
						}
					}

					// If there is no public key recieved we dont de-crypt the message 
					if (remotePublicKey == "" && !recievedText.Contains("#publicKeyStarts#"))
					{
						// Invoke. Casting to action makes it act as a delegate, allowing thread safe operations.
						RunOnUiThread (() => txtMessages.Append("\nRemote: [uncrypted]\n\t" + recievedText));
						RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length()));
					}
					else if (!recievedText.Contains("#publicKeyStarts#"))
					{
						RunOnUiThread (() => txtMessages.Append("\nRemote: [crypted]\n\t" + crypto.DecryptMessage(recievedText)));
						RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length()));
					}
				}
			}
			catch (Exception ex)
			{
				// log errors
				RunOnUiThread (() => txtMessages.Append("\n" + ex.Message));
				RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length()));
			}
		}
	}
}


