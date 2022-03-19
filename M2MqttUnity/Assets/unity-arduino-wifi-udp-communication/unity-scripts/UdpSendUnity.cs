﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UdpSendUnity : MonoBehaviour {

	private Socket sock;
	private List<Kiosk> kiosks = new List<Kiosk>();
	private string arduinoIP = "127.0.0.1";//"172.19.45.96";
	private int arduinoPort = 1234;




	void Send (IPAddress ip, int port, string msg) {
		
		IPAddress serverAddr = ip;
		IPEndPoint endPoint = new IPEndPoint(serverAddr, port);
		string text = msg;
		byte[] send_buffer = Encoding.ASCII.GetBytes(text);
		
		sock.SendTo(send_buffer, endPoint);
	}

	public void SendToKiosk (string name, string msg) {
		for (int i=0; i<kiosks.Count; i++) {
			Kiosk k = kiosks[i];
			if (k.name == name) {
				Send(k.ip, k.port, msg);
				break;
			}
		}
	}

	public void AddNewKiosk (Kiosk k) {
		bool exists = false;
		for (int i=0; i<kiosks.Count; i++) {
			if (kiosks[i].name == k.name) {
				exists = true;
				break;
			}
		}
		if (!exists) {
			Debug.Log("Added new Kiosk : "+k.name+"\n");
			kiosks.Add(k);
			Send(k.ip, k.port, "REGISTER_OK");
		}
	}

	// Use this for initialization
	void Start () {
		sock = new Socket(
			AddressFamily.InterNetwork,
			SocketType.Dgram,
			ProtocolType.Udp
		);
		Debug.Log ("Sender: Socket made\n");


		IPAddress ip = IPAddress.Parse(arduinoIP);
		Send(ip, 8888, "Unity send");
		byte[] send_buffer = Encoding.ASCII.GetBytes("Unity send");
		print(send_buffer.Length);
	}

	// Update is called once per frame
	void Update () {
		/*if (Input.GetKeyDown("space")) {
			Debug.Log("Sender: Flashing all kiosks\n");
			for (int i=0; i<kiosks.Count; i++) {
				Kiosk k = kiosks[i];
				Send(k.ip,k.port,"FLASH_LED");
			}
		}*/

	/*	IPAddress ip = IPAddress.Parse(arduinoIP);
		Send(ip, 8888, "Unity send");
*/

	}

	public void space(InputAction.CallbackContext context)
    {
		
			Debug.Log("press");
			IPAddress ip = IPAddress.Parse(arduinoIP);
			Send(ip, arduinoPort, "Unity send");
		
		
	}
}
