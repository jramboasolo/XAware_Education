using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using Microsoft.MixedReality.WebRTC.Unity;
using Vuforia;

//Class to manage the streaming in the application but only for the current scene
//So the streaming stops when the user changes of scene and he needs to restart the streaming in the new scene
// For the streaming, we use Mixed reality WebRTC, with the tutorial step up, so one upgrade is to change the signaler
// Warning: Vuforia monopolizes the camera, so WebRTC and Vuforia cannot use the camera simultaneously
// Behavior, if we start the streaming, vuforia stops so we cannot use the tracking or the detection
public class Streaming : MonoBehaviour
{
    private Thread streamingThread;
    private bool isWorking,isStreaming,isConnected,errorConnection,canceled; //Boolean to manage the thread
    private static EventWaitHandle myEvent; 
    string ipServer;
    private int port,address;
    private TcpClient client; // tcp connection for the streaming. Not the UDP because the data are too big for this protocol
    private static int bufferSize = 1024;
    private NetworkStream stream;
    private byte[] bufferReceive,sendBuffer;
    private int frame;
    private GameObject videoPlayer,myPeerConnection,mySignaler;
    public bool targetFound { set; private get;}

    // Start is called before the first frame update
    private void Start()
    {
        ipServer = "192.168.1.107";
		port = 8080;
        address = 100;
        isStreaming = false;
        targetFound = true;
        errorConnection = true;
        canceled = false;
        frame = 0;
        videoPlayer = GameObject.Find("VideoPlayer");
        myPeerConnection = GameObject.Find("myPeerConnection");
        mySignaler = GameObject.Find("mySignaler");
        if(mySignaler!=null)
            mySignaler.GetComponent<NodeDssSignaler>().HttpServerAddress = string.Format("http://{0}:3000/","192.168.1.103");
        myEvent = new EventWaitHandle(false,EventResetMode.AutoReset);;
        streamingThread = new Thread(new ThreadStart(StreamingControl));
        streamingThread.IsBackground = true;
        isWorking = false;
        isConnected = false;
        streamingThread.Start();
        StartCoroutine(SetSignalerAdress());

    }

    //Main function for the thread, connect to a tcp server, wait for data and send them to the server
    private void StreamingControl()
    {
        isWorking = true;
        Connect(ipServer,port);
        while(isWorking)
        {
            myEvent.WaitOne();
        }
        ClientClose();
    }

    //Function for the connection with a tcp server
    private void Connect(string _ipServer, int _port)
    {
        do
        {
            try
            {
                client = new TcpClient(ipServer,port);
                errorConnection = false;
                client.ReceiveBufferSize = bufferSize;
                client.SendBufferSize = bufferSize;
                bufferReceive = new byte[bufferSize];
                sendBuffer = new byte[bufferSize];
                stream = client.GetStream();
                isConnected = true;
                stream.BeginRead(bufferReceive,0,bufferSize,ReceiveCallback,null);
            }
            catch (Exception )
            {
                address += 1;
                if(address >= 120)
                {
                    errorConnection = false;
                    isConnected = true;
                    canceled = true;
                }
				ipServer = string.Format("192.168.1.{0}",address);
            }
        }while(errorConnection); //loop to find the ip server address
    }

    public void ConnectedServer()
    {
        if(!isConnected)
        {
            myEvent.Set();
        }    
    }

    //Callback to manage the reception of message from the server
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

			byte[] dataReceive = new byte[byteLength];
			Array.Copy(bufferReceive, dataReceive, byteLength);

			string message = Encoding.ASCII.GetString(dataReceive);

            if(message == "1")
            {
                isStreaming = true; // Begin the stream
                frame = 0;  
            }
            if(message == "0")
            {
                isStreaming = false; //End the stream
            }

			stream.BeginRead(bufferReceive, 0, bufferSize, ReceiveCallback, null);
		}
		catch
		{
			ClientClose();
		}
    }

    //Funtion to close a tcp client
    private void ClientClose()
	{
		if(stream != null)
			stream.Close();
		if(client != null)
			client.Close();
		client = null;
		isConnected = false;
	}

    //Return the boolean IsConnected
    private bool ServerConnected()
    {
        return isConnected;
    }

    //Coroutine to update the Signaler address when the tcp client has been succefully connected to the server
    private IEnumerator SetSignalerAdress()
    {
        yield return new WaitUntil(ServerConnected);
        if(mySignaler!=null)
            mySignaler.GetComponent<NodeDssSignaler>().HttpServerAddress = string.Format("http://{0}:3000/",ipServer);
        if(canceled)
            Destroy(GameObject.Find("StreamingManager"));
    }

    private void Update()
    {
        if(targetFound)
        {
            if(isStreaming & frame==0)
            {
                frame = 1;
                myPeerConnection.GetComponent<PeerConnection>().enabled = true;
            }
            if(!isStreaming & frame!=0)
            {
                myPeerConnection.GetComponent<PeerConnection>().enabled = false;
                frame = 0;
            }
        }
        else
        {
            if(frame!=0)
            {
                myPeerConnection.GetComponent<PeerConnection>().enabled = false;
                frame = 0;
            }
        }
    }

    //Function called when the client tcp receive a "1" and begin the streaming
    public void StartStreaming()
    {
        StartCoroutine(StopVuforia());
    }

    //Function called when the client tco receive a "0" and stop the streaming
    public void StopStreaming()
    {  
        StartCoroutine(StartVuforia());
    }

    //Function to stop the vuforia's camera when the stream begins
    private IEnumerator StopVuforia()
    {   
        CameraDevice.Instance.Stop();
        yield return 2;
        try
        {
            videoPlayer.GetComponent<WebcamSource>().enabled = true;
        }
        catch
        {

        }
    }
    
    //Function to restart the vuforia's camera when the stream stops
    private IEnumerator StartVuforia()
    {
        yield return 4;
        try
        {
            GameObject.Find("Main Camera").GetComponent<VuforiaBehaviour>().enabled = false;
            GameObject.Find("Main Camera").GetComponent<VuforiaBehaviour>().enabled = true;
        }
        catch
        {   

        }
    }


    //Stops the thread and releases the resource
    //Unity Function called when the object is destroyed
    private void OnDestroy()
    {
        isStreaming = false;
        isWorking = false;
        errorConnection = false;
        if(myEvent!=null)
            myEvent.Set();
    }
}
