using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

// Class to manage a tcp client for the application
// This tcp client is used for the connection with the real C-arm
// The connection with the real C-arm is made in two steps: the connection between a server and the real C-arm using a Rabbit client, and the connection between the server and the HoloLens
// Two steps because the computer of the C-arm can be accessed only with an ethernet cable connection
// Principle: the server retrieves the information from the C-arm computer and sends the Lao and Cran to the HoloLens
// Data send through a struct (int angleTheta and int anglePhi), so the server sends only 8 bits for one update (when Lao or Cran has changed), and so the client (HoloLens) needs to receive just 8 bits at a time
// Allow to know how many bits to read on the stream and avoid some errors when the server sends a lot of information successively
public class TcpClientHoloLens : MonoBehaviour
{
	//Reference to the Zeego, buttons for the connection and deconnection with the remote Zeego
    private GameObject zeegoPretty,buttonEight,buttonEightBis,GameSystem,zeegoPrettyPhantom,infoConnection;
    private Thread threadTcp;
    private TcpClient client;
	private string messageReceive;
	public string ipServer { set; private get;}
	private int port,address;

	//Boolean to manage the thread and the update of the Zeego's rotation
	private bool stopThread,EndConnection,newMessage,errorConnection,cancelConnection;
	private NetworkStream stream;

	//Objet uses to keep waiting the thread
	private static EventWaitHandle myEvent;
	private static int bufferSize = 8;
	private byte[] receiveBuffer;
	private ZeegoInfo zeegoInfo;
	private ZeegoManagement zeegoManagement,zeegoManagementPhantom;
	private BoxCollider[] colliders;

	private TextMeshPro infoConnectionText;

    // Start is called before the first frame update
    private void Start()
    {
        ipServer = "192.168.1.100"; //ip address of the server
		address = 100;
		port = 8052;
		zeegoPretty = GameObject.Find("ZeegoPretty");
		zeegoPrettyPhantom = GameObject.Find("ZeegoPrettyPhantom");
		buttonEight = GameObject.Find("ButtonEight");
		infoConnection = GameObject.Find("InfoConnection");
		if(infoConnection!=null)
		{
			infoConnectionText = GameObject.Find("ResultatInfoConnection").GetComponent<TextMeshPro>();
			infoConnection.SetActive(false); // If the object InfoConnection exists at the beginning of the game, we deactivate this object so the user cannot see it anymore but the script still has the reference
		}
		stopThread = false;
		EndConnection = false;
		errorConnection = true;
		cancelConnection = false;
		zeegoInfo = new ZeegoInfo();
		zeegoManagement = zeegoPretty.GetComponent<ZeegoManagement>();
		if(zeegoPrettyPhantom!=null)
			zeegoManagementPhantom = zeegoPrettyPhantom.GetComponent<ZeegoManagement>();
		colliders = zeegoPretty.GetComponents<BoxCollider>();
		myEvent = new EventWaitHandle(false,EventResetMode.AutoReset);
		threadTcp = new Thread(new ThreadStart(TcpConnection));
		GameSystem = GameObject.Find("SceneManagementObject");
		threadTcp.IsBackground = true;
		threadTcp.Start();
    }

    // Update is called once per frame
	// Check if there is a new message from the server and update the Zeego's rotation
	// Use this function because you can't access to a game object and these components in another thread than the main thread
    private void Update()
    {
		if(EndConnection)
		{
			buttonEight.SetActive(true);
			buttonEightBis = GameObject.Find("ButtonEightBis");
			try
			{
				buttonEightBis.SetActive(false);
			}
			catch
			{

			}
			EndConnection = false;
		}
		if(newMessage)
		{
			zeegoManagement.RotateZeego(-zeegoInfo.AngleTheta,-zeegoInfo.AnglePhi,false);
			if(zeegoManagementPhantom!=null)
				zeegoManagementPhantom.RotateZeego(-zeegoInfo.AngleTheta,-zeegoInfo.AnglePhi,true);
			newMessage = false;
		}
    }

	//Function called by the thread and manage the TCP connection
    private void TcpConnection()
    {
         while(!stopThread)
		 {
			 myEvent.WaitOne();
			 if(client == null & !stopThread)
			 {
				 do
				 {
					 try
					{
						client = new TcpClient(ipServer,port);
						errorConnection = false;
						client.ReceiveBufferSize = bufferSize;
						receiveBuffer = new byte[bufferSize];

						stream = client.GetStream();

						stream.BeginRead(receiveBuffer,0,bufferSize,ReceiveCallback,null);
					}
					catch (SocketException )
					{
						if(!cancelConnection)
						{
							errorConnection = true;
							address += 1;
							ipServer = string.Format("192.168.1.{0}",address);
						}
						else
						{
							errorConnection = false;
						}
					} 
				 }while(errorConnection); // loop to find the server's ip address
			 }
			 else
			 {
				 ClientClose();
			 }
		 }
    }

	//Callback to manage the reception of a message from the server
    private void ReceiveCallback(IAsyncResult result)
    {
        try
		{
			int byteLength = stream.EndRead(result);
			if(byteLength <= 0)
			{
				ClientClose();
				return;
			}
			
			byte[] data = new byte[byteLength];
			Array.Copy(receiveBuffer, data, byteLength);
			messageReceive = Encoding.ASCII.GetString(data);
			if(data.Length == bufferSize)
			{
				zeegoInfo.AngleTheta = BitConverter.ToInt32(data,0);
				zeegoInfo.AnglePhi = BitConverter.ToInt32(data,4);
				newMessage = true;
			}
			stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
		}
		catch
		{
			ClientClose();
		}
    }

	//Function to close the tcp client
	private void ClientClose()
	{
		if(stream != null)
			stream.Close();
		if(client != null)
			client.Close();
		client = null;
		EndConnection = true;
	}

	// Function called when the HoloLens is no longer connected to the server
	// Enable all the Zeego's colliders
    public void StopConnection()
	{
		myEvent.Set();
		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = true;
		}
		zeegoManagement.isRealZeego = false;
	}

	//Function to cancel the waiting connection
	public void CancelConnection()
	{
		EndConnection = true;
		cancelConnection = true;
		if(infoConnectionText!=null)
			infoConnectionText.text = "Wait cancel";
	}

	// Stop the thread and release the resource
    // Unity Function called when the object is destroyed
    private void OnDestroy()
	{
		stopThread = true;
		myEvent.Set();
	}

	// Function called when the HoloLens is connected to the server
	// Disable all the Zeego's colliders
	public void ConnectedServer()
	{
		myEvent.Set();
		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = false;
		}
		zeegoManagement.isRealZeego = true;
		infoConnection.SetActive(true);
		StartCoroutine(ShowInfoConnection());
	}

	//Return boolean errorConnection
	private bool IsConnected()
	{
		return errorConnection;
	}

	//Coroutine used to show and update the current connection's status
	private IEnumerator ShowInfoConnection()
	{
		yield return new WaitWhile(IsConnected);
		if(infoConnectionText!=null)
		{
			if(cancelConnection)//If the user cancels the connection, show the appropiate message
			{
				infoConnectionText.text = "Connection aborted";
				address = 100;
				zeegoManagement.isRealZeego = false;
			}
			else
			{
				infoConnectionText.text = "You are connected";
			}
			yield return new WaitForSecondsRealtime(2);
			infoConnectionText.text = "Try to connect to the C-arm...";
		}
		cancelConnection = false;
		errorConnection = true;
		infoConnection.SetActive(false);
	
	}
}

[Serializable]
// Intern class for the json deserialization
internal class ZeegoInfo
{
	public int AngleTheta;
	public int AnglePhi;
}