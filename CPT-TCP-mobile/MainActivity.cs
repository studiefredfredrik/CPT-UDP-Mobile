using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Android.Text;


namespace CPTTCPmobile
{
	[Activity (Label = "CryptoPeerTalk for Android", MainLauncher = true, Icon = "@drawable/icon")]
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

		const int port = 2280;
		string recieverIP = "";
		UdpClient udpServer;

		string separator = "\n\t";
		string htmlSeparator = "<br>&thinsp;";
		string fontStartBlue = "<font color=grey>";
		string fontStopBlue = "</font>";

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
						udpServer.Connect(new IPEndPoint(IPAddress.Parse(txtIP.Text),port));
						recieverIP = txtIP.Text;
						string knock = "#holepunch#"; // We open the incoming port 2280 by sending at it
						ASCIIEncoding asen = new ASCIIEncoding();
						byte[] ba = asen.GetBytes(knock);
						udpServer.Send(ba, ba.Length);
						txtMessages.Append("\n[recieved knock]");
						txtMessages.SetSelection(txtMessages.Length());
					}
					catch(Exception ex)
					{
						txtMessages.Append("\n" + ex.Message);
					}
				}
				else if(btnConnect.Text == "Close")
				{
					udpServer.Close();
					System.Environment.Exit(0);
				}
			};

			btnEncrypt.Click += (object sender, EventArgs e) =>
			{
				// Start encrypted mode on connect
				string wrappedKey = "#publicKeyStarts#" + crypto.publicKey + "#publicKeyStops#";
				udpSend(wrappedKey);
				txtMessages.Append("\n[sent public key]");
				txtMessages.SetSelection(txtMessages.Length());
				keySendt = true;
			};
			btnSend.Click += (object sender, EventArgs e) =>
			{
				try
				{
					if (remotePublicKey == "")
					{
						txtMessages.Append(Html.FromHtml("<br>" + fontStartBlue+ "<br>You: [uncrypted]" + htmlSeparator + "&emsp;" + txtSend.Text + fontStopBlue));
						txtMessages.SetSelection(txtMessages.Length());
						// send message unencrypted
						udpSend(txtSend.Text);
						txtSend.Text = "";
					}
					else
					{
						txtMessages.Append(Html.FromHtml("<br>" + fontStartBlue+ "<br>You:" + htmlSeparator + txtSend.Text + fontStopBlue));
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
							txtMessages.Append(Html.FromHtml("<br>" + fontStartBlue+ "<br>You: [uncrypted]" + htmlSeparator + "&emsp;" + txtSend.Text + fontStopBlue));
							txtMessages.SetSelection(txtMessages.Length());
							// send message unencrypted
							udpSend(txtSend.Text);
							txtSend.Text = "";
						}
						else
						{
							txtMessages.Append(Html.FromHtml("<br>" + fontStartBlue+ "<br>You:" + htmlSeparator + txtSend.Text + fontStopBlue));
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
							udpServer.Connect(new IPEndPoint(IPAddress.Parse(txtIP.Text),port));
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
				udpServer = new UdpClient(port);
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

					if(recieverIP == "") 
					{
						udpServer.Connect(remoteEndPoint);
						recieverIP = remoteEndPoint.Address.ToString(); // Update the reciever IP if new connection
						RunOnUiThread (() => btnConnect.Text = "Close");
						RunOnUiThread (() => txtIP.Text = recieverIP);
					}

					if(recievedText.Contains("#holepunch#"))
					{
						RunOnUiThread (() => txtMessages.Append("\n[recieved knock]"));
						RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length())); // scroll to end
						continue;
					}
					// Check for public key in the message
					if (recievedText.Contains("#publicKeyStarts#"))
					{
						remotePublicKey = Toolbox.GetStringBetweenStrings(recievedText, "#publicKeyStarts#", "#publicKeyStops#");
						RunOnUiThread (() => txtMessages.Append("\n[recieved public key]"));
						RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length())); // scroll to end
						if(!keySendt)
						{
							// send key if not sent
							string wrappedKey = "#publicKeyStarts#" + crypto.publicKey + "#publicKeyStops#";
							udpSend(wrappedKey);
							RunOnUiThread (() => txtMessages.Append("\n[sent public key]"));
							RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length()));
							keySendt = true;
						}
					}

					// If there is no public key recieved we dont de-crypt the message 
					if (remotePublicKey == "" && !recievedText.Contains("#publicKeyStarts#"))
					{
						// Invoke. Casting to action makes it act as a delegate, allowing thread safe operations.
						RunOnUiThread (() => txtMessages.Append("\nRemote: [uncrypted]"+separator + recievedText));
						RunOnUiThread (() => txtMessages.SetSelection(txtMessages.Length()));
					}
					else if (!recievedText.Contains("#publicKeyStarts#"))
					{
						RunOnUiThread (() => txtMessages.Append("\nRemote:"+separator + crypto.DecryptMessage(recievedText)));
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


