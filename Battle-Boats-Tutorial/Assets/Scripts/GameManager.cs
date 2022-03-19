using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviour
{
    [Header("Ships")]
    public GameObject[] ships;
    public EnemyScript enemyScript;
    private ShipScript shipScript;
    private List<int[]> enemyShips;
    private int shipIndex = 0;
    public List<TileScript> allTileScripts;    

    [Header("HUD")]
    public Button nextBtn;
    public Button rotateBtn;
    public Button replayBtn;
    public Text topText;
    public Text playerShipText;
    public Text enemyShipText;

    [Header("Objects")]
    public GameObject missilePrefab;
    public GameObject enemyMissilePrefab;
    public GameObject firePrefab;
    public GameObject woodDock;

    private bool setupComplete = false;
    private bool playerTurn = true;
    
    private List<GameObject> playerFires = new List<GameObject>();
    private List<GameObject> enemyFires = new List<GameObject>();
    
    private int enemyShipCount = 5;
    private int playerShipCount = 5;

    [Header("VR")]
    [SerializeField] GameObject rightHand;


    //UDP
    private UDP udp;
    private static int BYTE_ARRAY_LENGTH = 4;
    private byte[] sendData = new byte[BYTE_ARRAY_LENGTH];
    private byte[] receiveDat = new byte[BYTE_ARRAY_LENGTH];

    private byte msgTypeByte;
    private byte gamePhaseByte;
    private byte hitMissByte;
    private byte tileByte;

    private int GUESS = 0;
    private int INFO = 1;

    private int MISS = 0;
    private int HIT = 1;
    private int SUNK = 2;

    private int PHYSICAL_WINS = 0;
    private int VR_WINS = 1;

    private int msgTypeInt;
    private int gamePhaseInt;
    private int hitMissInt;
    private int tileInt;

    private GameObject guessedTile;
    private void Update()
    {
        if (setupComplete)
        {
            if (udp.clientAvailable())
            {
                if (!playerTurn)
                {
                    //wait for incoming data
                    do
                    {
                        receiveDat = udp.receiveData();
                    } while (receiveDat.Length < BYTE_ARRAY_LENGTH);

                    decodBytes(receiveDat);
                    visualizeGuessResult();
                }
                else
                {
                    //Need to receive enemy guess and return info
                    do
                    {
                        receiveDat = udp.receiveData();
                    } while (receiveDat.Length < BYTE_ARRAY_LENGTH);
                    decodBytes(receiveDat);
                    enemyScript.NPCTurn(tileInt);

                }
            }
            
        }

    }

    private void visualizeGuessResult()
    {
        var tile = guessedTile;
        if(hitMissInt == MISS)
        {
            tile.GetComponent<TileScript>().SetTileColor(1, new Color32(38, 57, 76, 255));
            tile.GetComponent<TileScript>().SwitchColors(1);
            topText.text = "Missed, there is no ship there.";
        }
        else if(hitMissInt == HIT)
        {
            topText.text = "HIT!!";
            tile.GetComponent<TileScript>().SetTileColor(1, new Color32(255, 0, 0, 255));
            tile.GetComponent<TileScript>().SwitchColors(1);
        }
        else if(hitMissInt == SUNK)
        {
            enemyShipCount--;
            topText.text = "SUNK!!!!!!";
            enemyFires.Add(Instantiate(firePrefab, tile.transform.position, Quaternion.identity));
            tile.GetComponent<TileScript>().SetTileColor(1, new Color32(68, 0, 0, 255));
            tile.GetComponent<TileScript>().SwitchColors(1);
        }
     
    }

    

    // Start is called before the first frame update
    void Start()
    {
        this.udp = GetComponent<UDP>();

        shipScript = ships[shipIndex].GetComponent<ShipScript>();
        nextBtn.onClick.AddListener(() => NextShipClicked());
        rotateBtn.onClick.AddListener(() => RotateClicked());
        replayBtn.onClick.AddListener(() => ReplayClicked());


        //TODO take out completely and wait for knowledge of hit or miss
        //enemyShips = enemyScript.PlaceEnemyShips();
    }

    void OnActivate()
    {
        RaycastHit hit;
        if (rightHand.GetComponent<XRRayInteractor>().TryGetCurrent3DRaycastHit(out hit))
        {
            string obj = hit.collider.gameObject.name;
            print(obj);
            if (obj.Equals("RotateBtn"))
            {
                RotateClicked();
            }
            else if (obj.Equals("NextBtn"))
            {
                NextShipClicked();
            }
            else if (obj.Equals("ReplayBtn"))
            {
                ReplayClicked();
            }
        }
    }

    private void NextShipClicked()
    {
        if (!shipScript.OnGameBoard())
        {
            shipScript.FlashColor(Color.red);
        } else
        {
            if(shipIndex <= ships.Length - 2)
            {
                shipIndex++;
                shipScript = ships[shipIndex].GetComponent<ShipScript>();
                shipScript.FlashColor(Color.yellow);
            }
            else
            {
                rotateBtn.gameObject.SetActive(false);
                nextBtn.gameObject.SetActive(false);
                woodDock.SetActive(false);
                topText.text = "Guess an enemy tile.";
                setupComplete = true;
                for (int i = 0; i < ships.Length; i++) ships[i].SetActive(false);
            }
        }
        
    }

    public void TileClicked(GameObject tile)
    {
        if(setupComplete && playerTurn)
        {
            Vector3 tilePos = tile.transform.position;
            tilePos.y += 15;
            playerTurn = false;
            Instantiate(missilePrefab, tilePos, missilePrefab.transform.rotation);
        } else if (!setupComplete)
        {
            PlaceShip(tile);
            shipScript.SetClickedTile(tile);
        }
    }

    private void PlaceShip(GameObject tile)
    {
        shipScript = ships[shipIndex].GetComponent<ShipScript>();
        shipScript.ClearTileList();
        Vector3 newVec = shipScript.GetOffsetVec(tile.transform.position);
        ships[shipIndex].transform.localPosition = newVec;
    }

    void RotateClicked()
    {
        shipScript.RotateShip();
    }

    public void CheckHit(GameObject tile)
    {
        guessedTile = tile;
        int tileNum = Int32.Parse(Regex.Match(tile.name, @"\d+").Value);
        /*int hitCount = 0;
        foreach(int[] tileNumArray in enemyShips)
        {
            if (tileNumArray.Contains(tileNum))
            {
                for (int i = 0; i < tileNumArray.Length; i++)
                {
                    if (tileNumArray[i] == tileNum)
                    {
                        tileNumArray[i] = -5;
                        hitCount++;
                    }
                    else if (tileNumArray[i] == -5)
                    {
                        hitCount++;
                    }
                }
                if (hitCount == tileNumArray.Length)
                {
                    enemyShipCount--;
                    topText.text = "SUNK!!!!!!";
                    enemyFires.Add(Instantiate(firePrefab, tile.transform.position, Quaternion.identity));
                    tile.GetComponent<TileScript>().SetTileColor(1, new Color32(68, 0, 0, 255));
                    tile.GetComponent<TileScript>().SwitchColors(1);

                    //UDP
                    hitMissByte = Convert.ToByte(SUNK);
                }
                else
                {
                    topText.text = "HIT!!";
                    tile.GetComponent<TileScript>().SetTileColor(1, new Color32(255, 0, 0, 255));
                    tile.GetComponent<TileScript>().SwitchColors(1);

                    //UDP
                    hitMissByte = Convert.ToByte(HIT);
                }
                break;
            }
            
        }
        if(hitCount == 0)
        {
            tile.GetComponent<TileScript>().SetTileColor(1, new Color32(38, 57, 76, 255));
            tile.GetComponent<TileScript>().SwitchColors(1);
            topText.text = "Missed, there is no ship there.";

            //UDP
            hitMissByte = Convert.ToByte(MISS);

        }*/

        //UDP
        msgTypeByte = Convert.ToByte(GUESS);
        tileByte = Convert.ToByte(tileNum);
        

        Invoke("EndPlayerTurn", 1.0f);
    }

    public void EnemyHitPlayer(Vector3 tile, int tileNum, GameObject hitObj)
    {
        enemyScript.MissileHit(tileNum);
        tile.y += 0.2f;
        playerFires.Add(Instantiate(firePrefab, tile, Quaternion.identity));
        if (hitObj.GetComponent<ShipScript>().HitCheckSank())
        {
            //UDP sunk
            hitMissByte = Convert.ToByte(SUNK);

            playerShipCount--;
            playerShipText.text = playerShipCount.ToString();
            enemyScript.SunkPlayer();
        }

        //UDP hit
        hitMissByte = Convert.ToByte(HIT);
       Invoke("EndEnemyTurn", 2.0f);

        //TODO update info
    }

    private void EndPlayerTurn()
    {
        for (int i = 0; i < ships.Length; i++) ships[i].SetActive(true);
        foreach (GameObject fire in playerFires) fire.SetActive(true);
        foreach (GameObject fire in enemyFires) fire.SetActive(false);
        enemyShipText.text = enemyShipCount.ToString();
        topText.text = "Enemy's turn";
        
        //Send Guess to P2
        sendData = encodeBytes();
        udp.sendData(sendData);

/*//Player 2 visualizes guess + makes their move

        //Receive info from P2 on their guess
        receiveDat = udp.receiveData();
        decodBytes(receiveDat);
        if(gamePhaseInt == PHYSICAL_WINS)
        {
            GameOver("ENEMEY WINs!!!");
        }

        //Takes the tile you are targeting: 0 is bottom left, count up to the right 
        enemyScript.NPCTurn(tileInt);*/


        ColorAllTiles(0);
        if (playerShipCount < 1)
        {
            GameOver("ENEMY WINs!!!");

            //UDP
            gamePhaseByte = Convert.ToByte(PHYSICAL_WINS);
        }
        playerTurn = false;

    }

    public void EndEnemyTurn()
    {
        for (int i = 0; i < ships.Length; i++) ships[i].SetActive(false);
        foreach (GameObject fire in playerFires) fire.SetActive(false);
        foreach (GameObject fire in enemyFires) fire.SetActive(true);
        playerShipText.text = playerShipCount.ToString();
        topText.text = "Select a tile";
        playerTurn = true;
        ColorAllTiles(1);
        if (enemyShipCount < 1)
        {
            GameOver("YOU WIN!!");

            //UDP
            gamePhaseByte = Convert.ToByte(VR_WINS);
        }

        //UDP
        udp.sendData(encodeBytes());
        //playerTurn flag flipped so clicking on next tile starts the player turn 
    }

    private void ColorAllTiles(int colorIndex)
    {
        foreach (TileScript tileScript in allTileScripts)
        {
            tileScript.SwitchColors(colorIndex);
        }
    }

    void GameOver(string winner)
    {
        topText.text = "Game Over: " + winner;
        replayBtn.gameObject.SetActive(true);
        playerTurn = false;

        //TODO update data
    }

    void ReplayClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private byte[] encodeBytes()
    {
        byte[] data = new byte[BYTE_ARRAY_LENGTH];
        data[0] = msgTypeByte;
        data[1] = gamePhaseByte;
        data[2] = hitMissByte;
        data[3] = tileByte;
        return data;
    }

    private void decodBytes(byte[] input)
    {
        msgTypeInt = Convert.ToInt32(input[0]);
        gamePhaseInt = Convert.ToInt32(input[1]);
        hitMissInt = Convert.ToInt32(input[2]);
        tileInt = Convert.ToInt32(input[3]);
    }

    public void setHitMissBytes(int status)
    {
        hitMissByte = Convert.ToByte(status);
    }
}
