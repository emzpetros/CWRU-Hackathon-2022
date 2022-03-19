using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
public class UDP2 : MonoBehaviour
{
    UdpClient client;
    private int count = 0;
    void Start()
    {
        UDPTest();
    }
    void UDPTest()
    {
         client = new UdpClient(1234);
        try
        {
            client.Connect("172.19.45.96", 1234);
            byte[] sendBytes = Encoding.ASCII.GetBytes("Hello, from the client");
            client.Send(sendBytes, sendBytes.Length);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 8888);

            if(client.Available>0)
            {
                byte[] receiveBytes = client.Receive(ref remoteEndPoint);
                string receivedString = Encoding.ASCII.GetString(receiveBytes);
                Debug.Log("Message received from the server " + receivedString);
            }
            
        }
        catch (Exception e)
        {
            print("Exception thrown " + e.Message);
            }
        }

        private void Update()
    {
        if(client.Available > 0)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 8888);
            byte[] receiveBytes = client.Receive(ref remoteEndPoint);
            //directly index array
            string receivedString = Encoding.ASCII.GetString(receiveBytes);
            print("Message received from the server " + receivedString);
        }
        

        byte[] sendBytes = Encoding.ASCII.GetBytes((count++).ToString());
        client.Send(sendBytes, sendBytes.Length);

    }
}