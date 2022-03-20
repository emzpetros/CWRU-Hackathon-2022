#include <WebServer.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <FastLED.h>

#define NUM_LEDS 50
#define DATA_PIN 13

//Joystick pin definitions
#define JOYX 36
#define JOYY 39
#define JOYB 34

//set up to connect to an existing network (e.g. mobile hotspot from laptop that will run the python code)
const char* ssid = "CaseGuest";
const char* password = "";

CRGB leds[200];

//ROW then COL, bottom of board is 0th row, left is 0th col
//shows which places you have guessed
// false for not guessed, true for guessed
//
//// truth table for locations of where your ships are
//boolean[10][10] ships = {{true, true, false, false, false, false, false, false, false, false},
//                       {true, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false},
//                       {false, false, false, false, false, false, false, false, false, false}};
//
//// array to represent which of your guesses have been. SHOWS UP ON TOP
//// 0 = not guessed, 1 = hit, 2 = miss
//int[10][10] hitmissenemy = {{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0}};
//int[10][10] hitmissfriend = {{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0},{0,0,0,0,0,0,0,0,0,0}};

//Physical player board: 5 offset
//board represting respresenting the VR player: 10 x 10

WiFiUDP Udp;
unsigned int localUdpPort = 1234;  //  port to listen on
char incomingPacket[255];  // buffer for incoming packets

const BYTE_ARRAY_LENGTH = 4;

byte sendData[BYTE_ARRAY_LENGTH];
byte receiveDat[BYTE_ARRAY_LENGTH];

//Communicaiton constants
const int GUESS = 0;
const int INFO = 1;

const int MISS = 0;
const int HIT = 1;
const int SUNK = 2;

const int PHYSICAL_WINS = 1;
const int VR_WINS = 2;

//UDP byte vars to be encoded
byte msgTypeByte;
byte gamePhaseByte;
byte hitMissByte;
byte tileByte;

///UDP int vars from decoding
int msgTypeInt;
int gamePhaseInt;
int hitMissInt;
int tileInt;

void setup()
{
  //initialize pins
  pinMode(JOYX, INPUT);
  pinMode(JOYY, INPUT);
  pinMode(JOYB, INPUT);

  // wifi shit
  int status = WL_IDLE_STATUS;
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  Serial.println("wifi begun");
  // Wait for connection
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("Connected to wifi");
  Udp.begin(localUdpPort);
  Serial.printf("Now listening at IP %s, UDP port %d\n", WiFi.localIP().toString().c_str(), localUdpPort);

  // we recv one packet from the remote so we can know its IP and port
  bool readPacket = false;
  while (!readPacket) {
    int packetSize = Udp.parsePacket();
    if (packetSize)
    {
      // receive incoming UDP packets
      Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
      int len = Udp.read(incomingPacket, 255);
      if (len > 0)
      {
        incomingPacket[len] = 0;
      }
      Serial.printf("UDP packet contents: %s\n", incomingPacket, Udp.remoteIP().toString().c_str(), Udp.remotePort());
      readPacket = true;
    }
  }
}
int i = 0;

#define UPPERTHRESH 3000
#define LOWERTHRESH 1000

void makeGuess() {
  boolean guessMade = false;
  int guessX = 4;
  int guessY = 4;
  
  while(!guessMade){
    //poll sensors
    int y = analogRead(JOYY);
    int x = analogRead(JOYX);

    if(x > UPPERTHRESH) {
      guessX++;  
    }
    else if (x < LOWERTHRESHOLD) {
      guessX--;  
    }

    if(y > UPPERTHRESH) {
      guessY++;  
    }
    else if (y < LOWERTHRESHOLD) {
      guessY--;  
    }
    
    if(digitalRead(JOYB) == 1) {
      guessMade = true;
    }
    delay(300);
  }

  Serial.print("x location: ");
  Serial.println(guessX);
  Serial.print("y location: ");
  Serial.println(guessY);

  //type is guess, phase and h/m are NA
  //send tile num form 0-99 
  msgTypeByte = byte(GUESS);
  gamePhaseByte = byte(0)
  hitMissByte = byte(MISS);
  tileByte = byte(coord2num(guessX, guessY));

}

int coord2num(int x, int y){
  int tileNum = (y * 9) + x;
  int rowOffset = y * 5;
  return tileNum + rowOffset;
}

void sendGuess () {
  //Concatenate bytes
  //TODO set byte vars throughout code
  sendData[0] = msgTypeByte;
  sendData[1] = gamePhaseByte;
  sendData[2] = hitMissByte;
  sendData[3] = tileByte;

  //send UDP
  Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
  Udp.printf(String(sendData).c_str(), 2);
  Udp.endPacket();
}

void waitForInfo() {}
void updateDisplay() {

}

void waitForGuess() {

}


void sendInfo() {
  msgTypeByte = byte(INFO);
  gamePhaseByte = byte(0)

//14 / row and -1 for array indexing 
  int x = (tileNum % 14) - 1;
  int y = (tileNum / 14) - 1;

  bool shipPresent = ships[y][x];
  if(!shipPresent){
    hitMissByte = byte(MISS);
    hitmissenemy[y][x] = 2;
  }
  else if(shipPresent){
    hitMissByte = byte(HIT);
    hitmissenemy[y][x] = 1;
  }
  // else if(shipSunk()){
  //   hitMissByte = byte(SUNK);

  // }
  
  //Set when we decode the incomming guess from the VR player
  tileByte = byte(tileInt);

  //check for win condition, sum of all hit tiles = total ship tiles (17)
  int totalHits = 0;
  for(int x = 0; x < 10; x++){
    for(int y = 0; y < 10; y++){
      if(hitmissfriend[y][x] == 1){
        totalHits++;
      }
    }
  }

  if(totalHits == 17){
    gamePhaseByte = byte(VR_WINS);
  }


  //Send data
  sendData[0] = msgTypeByte;
  sendData[1] = gamePhaseByte;
  sendData[2] = hitMissByte;
  sendData[3] = tileByte;

  //send UDP
  Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
  Udp.printf(String(sendData).c_str(), 2);
  Udp.endPacket();
  
}

bool shipSunk(){
  return;
}

//false = opponent turn
//true = our turn
boolean turnFlag = false;
/*
   if our turn, do turn and send packet
   otherwise, listen for packet, receive decode and update board.
*/
void loop()
{
  //if our turn
  if(turnFlag) {
    makeGuess();
    sendGuess();
    waitForInfo();
    updateDisplay();
    turnFlag = false;
  }
  //if enemy turn
  else {
    waitForGuess();
    sendInfo();
   // delay(500);
    turnFlag = true;
  }
  //sendPacket();
//  delay();
  listenForPacket();
  delay(100);
}

void sendPacket() {
    // once we know where we got the inital packet from, send data back to that IP address and port
  Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
  i = 0b00000001000000010000000101100011; 
  Udp.printf(String(i).c_str(), 2);
  Udp.endPacket();
//  Serial.println("packet sent");
//  Serial.println(Udp.remoteIP());
//  Serial.println(Udp.remotePort());
}

void listenForPacket() {
  int packetSize = Udp.parsePacket();
  if (packetSize)
  {
    // receive incoming UDP packets
    Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    int len = Udp.read(incomingPacket, 255);
    if (len > 0)
    {
      incomingPacket[len] = 0;
    }
    Serial.printf("UDP packet contents: %s\n", incomingPacket, Udp.remoteIP().toString().c_str(), Udp.remotePort());
    Serial.println("first index");
    Serial.println((int)incomingPacket[0]);
    Serial.println("index2");
    Serial.println((int)incomingPacket[1]);
    Serial.println("index3");
    Serial.println((int)incomingPacket[2]);
    Serial.println("index4");
    Serial.println((int)incomingPacket[3]);
  }
}



void initializeLEDS() {

}

/*
   loops thorugh and initializes guesses, ships, and hitmiss array.
   then, uses movement buttons to select ship placement

*/
void initializeBoard() {

}

#define IDENTIFIER     0b1000000000000
#define INIT           0b0100000000000
#define GAMEOVER       0b0010000000000
#define WINNER         0b0001000000000
#define HITMISS        0b0000100000000
#define ROW            0b0000011110000
#define COL            0b0000000001111


//void processHitMissMessage(int input) {
//  int identifier =     IDENTIFIER & input
//  int initialization = INIT & input
//  int gameover =       GAMEOVER & input
//  int winner =         WINNER & input
//  int hitmiss =        HITMISS & input
//  int row =            ROW & input
//  int col =            COL & input
//
//  Serial.println("identifier")
//  Serial.println(identifier)
//  Serial.println("initialization")
//  Serial.println(initialization)
//
//  Serial.println("gameover");
//  Serial.println(gameover);
//  Serial.println("winner");
//  Serial.println(winner);
//  Serial.println("hitmiss");
//  Serial.println(hitmiss);
//  Serial.println("row");
//  Serial.println(row);
//  Serial.println("col");
//  Serial.println(col);  
//}
