using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Get IP
using System.Net;
using System.Net.Sockets;
namespace CPTTCPmobile
{
	public class Toolbox
	{
			public static string GetStringBetweenStrings(string total, string startsAfter, string stopsBefore)
			{
				//string str = "super exemple of string key : text I want to keep - end of my string";
				int startIndex = total.IndexOf(startsAfter) + startsAfter.Length;
				int endIndex = total.IndexOf(stopsBefore);
				string newString = total.Substring(startIndex, endIndex - startIndex);

				return newString;
			}

			public static string getIP_External()
			{
				string externalIP = new WebClient().DownloadString("http://icanhazip.com");
				return externalIP;
			}

			public static string getIP_Local()
			{
				IPHostEntry host;
				string localIP = "";
				host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (IPAddress ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						localIP = ip.ToString();
						break;
					}
				}
				return localIP;
			}
		}
}

