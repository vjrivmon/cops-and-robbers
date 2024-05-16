﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        //Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (i - Constants.TilesPerRow >= 0) matriu[i, i - Constants.TilesPerRow] = 1; // Arriba
            if (i + Constants.TilesPerRow < Constants.NumTiles) matriu[i, i + Constants.TilesPerRow] = 1; // Abajo
            if (i % Constants.TilesPerRow != 0) matriu[i, i - 1] = 1; // Izquierda
            if (i % Constants.TilesPerRow != Constants.TilesPerRow - 1) matriu[i, i + 1] = 1; // Derecha
        }

        //Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                   // tiles[i].adjacency.Add(tiles[j]);
                    //robber.GetComponent<RobberMove>().MoveToTile(selectedTile.numTile);
                }
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }
    public int CalculateDistance(Tile tile1, Tile tile2)
    {
        int size = 8; // Tamaño del tablero (8x8)
    
        int x1 = tile1.numTile % size;
        int y1 = tile1.numTile / size;
    
        int x2 = tile2.numTile % size;
        int y2 = tile2.numTile / size;
    
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);
    }
    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        // Elegimos la casilla más lejana de cualquier policía entre las seleccionables
        Tile farthestTile = null;
        int maxDistance = -1;
        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                // Calculamos la distancia mínima a cualquier policía
                int minDistance = int.MaxValue;
                foreach (GameObject cop in cops)
                {
                    Tile copTile = tiles[cop.GetComponent<CopMove>().currentTile];
                    int distance = CalculateDistance(tile, copTile);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                // Si la distancia mínima es mayor que la máxima distancia actual, actualizamos la casilla más lejana y la máxima distancia
                if (minDistance > maxDistance)
                {
                    farthestTile = tile;
                    maxDistance = minDistance;
                }
            }
        }

        // Movemos al ladrón a la casilla más lejana
        if (farthestTile != null)
        {
            robber.GetComponent<RobberMove>().MoveToTile(farthestTile);
            robber.GetComponent<RobberMove>().currentTile = farthestTile.numTile;
        }
    }

    /*public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        List<Tile> selectableTiles = FindSelectableTiles(false);

        if (selectableTiles.Count > 0)
        {
            // Elegimos una casilla aleatoria entre las seleccionables
            int randomIndex = Random.Range(0, selectableTiles.Count);
            Tile randomTile = selectableTiles[randomIndex];

            // Movemos al caco a esa casilla
            robber.GetComponent<RobberMove>().MoveToTile(randomTile);

            // Actualizamos la variable currentTile del caco a la nueva casilla
            robber.GetComponent<RobberMove>().currentTile = randomTile.index;
        }
    }*/

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }


    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }
    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        //Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        nodes.Enqueue(tiles[indexcurrentTile]);
        tiles[indexcurrentTile].depth = 1; // Añadimos una profundidad inicial a la casilla actual
        while (nodes.Count > 0)
        {
            Tile current = nodes.Dequeue();
            current.visited = true;
            foreach (int tileIndex in current.adjacency)
            {
                Tile tile = tiles[tileIndex];
                if (!tile.visited && current.depth < 3) // Solo continuamos la búsqueda si la profundidad es menor a 3
                {
                    tile.parent = current;
                    tile.selectable = true;
                    tile.depth = current.depth + 1; // Incrementamos la profundidad para la siguiente casilla
                    nodes.Enqueue(tile);
                }
            }
        }
    }
}

