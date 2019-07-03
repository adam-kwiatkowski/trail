//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : MonoBehaviour {

    public Board Board;
    public GameObject TargetBlock;
    public GameObject Sphere;
    public float SphereSpeed;
    public GameObject[] PalettePositions;
    public Vector3 PaletteScale;
    public float verticalOffset;
    public Piece[] Pieces;

    //private GameObject[] _boardspaces;
    private List<GameObject> _boardspaces;
    private List<Block> _placedBlocks;
    //private List<GameObject> _placedGameObjects;
    private Block[,] _placedBlocksMap;

    private GameObject _selectedPiece;
    private Vector3 _selectedPieceCenterPointCache;
    private List<Piece> _piecesOnPalette;

    private Block _startingBlock;
    private Block _targetBlock;
    private GameObject _sphere;
    private List<Block> _path;
    private Vector3 _currentTargetPosition;
    private bool _newTargetBlockGenerated;

    // Use this for initialization
    void Start() {
        InitializeGame();
    }

    // Update is called once per frame
    void Update() {
        InputSystemsUpdate();
        GameplaySystemsUpdate();
    }

    void InitializeGame()
    {
        _placedBlocks = new List<Block>();
        _placedBlocksMap = new Block[Board.BoardWidth, Board.BoardHeight];
        _piecesOnPalette = new List<Piece>();
        _boardspaces = new List<GameObject>();
        GetAllBoardspaces();
        _placeStartingBlock();
        _placeTargetBlock();
        // tests:
    }

    void GetAllBoardspaces()
    {
        for (int i = 0; i < Board.transform.childCount; i++)
        {
            _boardspaces.Add(Board.transform.GetChild(i).gameObject);
        }
    }

    // Updates

    void InputSystemsUpdate()
    {
        MouseSelect();
        MovePieceByMouse();
        TryToPlacePiece();
        ReleaseSelectedPiece();
        TryToFindPath();
    }

    void GameplaySystemsUpdate()
    {
        RefreshPalette();
        GenerateNewTargetBlock();
        MoveSphere();
    }




    // Input Systems

    //1. Select: sprwdza, czy dany element da się zaznaczyć, jeśli tak, zaznacza go oraz wszystkie inne elementy z tej grupy.
    void MouseSelect()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                if (hit.collider != null && hit.collider.tag == "Block")
                {
                    _selectedPiece = hit.collider.gameObject.transform.parent.gameObject;
                    //_selectedPieceCenterPointCache = _selectedPiece.GetComponent<Piece>().CenterPoint;
                    _selectedPiece.transform.localScale = Vector3.one;
                }
        }
    }

    void MovePieceByMouse()
    {
        if (_selectedPiece != null)
        {
            Vector3 pointOnObjectCenter = new Vector3(_selectedPiece.transform.position.x, _selectedPiece.transform.position.y + (_selectedPiece.transform.localScale.y / 2), _selectedPiece.transform.position.z);
            Plane plane = new Plane(Vector3.up, pointOnObjectCenter);
            // create a ray from the mousePosition
            Ray ray2 = Camera.main.ScreenPointToRay(Input.mousePosition);
            // plane.Raycast returns the distance from the ray start to the hit point
            float distance;
            if (plane.Raycast(ray2, out distance))
            {
                // some point of the plane was hit - get its coordinates
                Vector3 hitPoint = ray2.GetPoint(distance);
                hitPoint.y -= (_selectedPiece.transform.localScale.y / 2);
                //hitPoint.x -= _selectedPieceCenterPointCache.x;
                //hitPoint.z -= _selectedPieceCenterPointCache.z;
                //_selectedPiece.transform.position = hitPoint;
                _selectedPiece.GetComponent<Piece>().SetCentralPosition(hitPoint);
            }
        }
    }

    void ReleaseSelectedPiece()
    {
        // tylko, jeśli coś aktualnie jest zaznaczone;
        if (Input.GetMouseButtonUp(0) && _selectedPiece != null)
        {
            /*
            Vector3 rounded = _selectedPiece.transform.position;
            rounded.x = Mathf.Round(rounded.x);
            rounded.z = Mathf.Round(rounded.z);
            _selectedPiece.transform.position = rounded;
            */
            /*** odczepianie i usuwanie piece holdera :)
            _selectedPiece.transform.DetachChildren();
            Destroy(_selectedPiece);
            */
            _selectedPiece.transform.localScale = PaletteScale;
            _selectedPiece.GetComponent<Piece>().SetCentralPosition(_selectedPiece.GetComponent<Piece>().PalettePosition);
            _selectedPiece = null;
        }
    }

    void TryToPlacePiece()
    {
        if (Input.GetMouseButtonUp(0) && _selectedPiece != null)
        {
            bool allPlaced = true;
            Dictionary<Transform, Vector3> newPositions = new Dictionary<Transform, Vector3>();
            for (int i = 0; i < _selectedPiece.transform.childCount; i++)
            {
                // create Ray
                //old:  Ray ray = new Ray(_selectedPiece.transform.GetChild(i).transform.position, Vector3.down);
                // Alternative Ray
                //Ray ray = new Ray(_selectedPiece.transform.GetChild(i).transform.position, Camera.main.transform.rotation * Vector3.left /*Vector3.Normalize(Camera.main.transform.rotation.eulerAngles) /*Quaternion.Inverse(Camera.main.transform.rotation) * Vector3.one*/);
                /*working:*/
                Ray ray = new Ray(_selectedPiece.transform.GetChild(i).transform.position, Vector3.Normalize(Camera.main.transform.rotation * Vector3.forward) /*Vector3.Normalize(Camera.main.transform.rotation.eulerAngles) /*Quaternion.Inverse(Camera.main.transform.rotation) * Vector3.one*/);


                // make raycast
                RaycastHit raycastHit;
                if (Physics.Raycast(ray, out raycastHit, PalettePositions[0].transform.position.y + 10) && raycastHit.collider.tag == "Boardspace")
                {
                    //Debug.Log("Yup");
                    // save new position in dictionary\
                    newPositions[_selectedPiece.transform.GetChild(i)] = new Vector3(raycastHit.collider.gameObject.transform.position.x, _selectedPiece.transform.GetChild(i).transform.localScale.y / 2 + 0.1F, raycastHit.collider.gameObject.transform.position.z);
                }
                else
                {
                    allPlaced = false;
                    //Debug.Log("Nope");
                }
            }
            if (allPlaced)
            {
                for (int i = 0; i < _selectedPiece.transform.childCount; i++)
                {
                    /*
                    Vector3 newPosition = _selectedPiece.transform.GetChild(i).transform.position;
                    newPosition.x = Mathf.Round(newPosition.x);
                    newPosition.y = _selectedPiece.transform.GetChild(i).transform.localScale.y / 2;
                    newPosition.z = Mathf.Round(newPosition.z);
                    _selectedPiece.transform.GetChild(i).transform.position = newPosition;
                    */
                    _selectedPiece.transform.GetChild(i).transform.position = newPositions[_selectedPiece.transform.GetChild(i)];
                    _selectedPiece.transform.GetChild(i).tag = "PlacedBlock";
                    Block block = _selectedPiece.transform.GetChild(i).GetComponent<Block>();
                    _placedBlocks.Add(block);
                    _placedBlocksMap[(int)newPositions[_selectedPiece.transform.GetChild(i)].x, (int)newPositions[_selectedPiece.transform.GetChild(i)].z] = block;
                }
                _selectedPiece.transform.DetachChildren();
                _piecesOnPalette.Remove(_selectedPiece.GetComponent<Piece>());
                Destroy(_selectedPiece);
                _selectedPiece = null;
            }
            //Ray ray = new Ra
        }
    }

    void TryToFindPath()
    {
        if (Input.GetMouseButtonUp(0) && _path == null || _newTargetBlockGenerated == true && _path == null)
        {
            //Debug.Log("_newTargetBlockGenerated: " + _newTargetBlockGenerated);
            _newTargetBlockGenerated = false;
            List<Block> path;
            bool pathFound = _findPath(_startingBlock, out path);
            Debug.Log("Find path: " + pathFound);
            if (pathFound)
            {
                foreach (Block step in path)
                {
                    Debug.Log(step.transform.position.x + "," + step.transform.position.z);
                }
                //path.RemoveAt(0);
                _path = path;
            } else
            {
                _path = null;
            }
        }
    }

    void MoveSphere()
    {
        if (_path != null)
        {
            // TEMP:
            _currentTargetPosition = new Vector3(_path[0].transform.position.x, _sphere.transform.position.y, _path[0].transform.position.z);
            // Porusz sferę do pierwszego kroku
            if (_sphere.transform.position == _currentTargetPosition)
            {
                if (_path.Count > 1)
                {
                    _placedBlocks.Remove(_path[0]);
                    _placedBlocksMap[(int)_path[0].transform.position.x, (int)_path[0].transform.position.z] = null;
                    Destroy(_path[0].gameObject);
                }
                _path.RemoveAt(0);
                if (_path.Count == 0)
                {
                    _path = null;
                    _startingBlock = _targetBlock;
                    _targetBlock = null;
                }
            }
            else
            {
                _sphere.transform.position = Vector3.MoveTowards(_sphere.transform.position, _currentTargetPosition, SphereSpeed);
            }
        }
    }

    // Gameplay Systems

    void GenerateNewTargetBlock()
    {
        if (_targetBlock == null)
        {
            _placeTargetBlock();
            _newTargetBlockGenerated = true;
        }
    }

    void RefreshPalette()
    {
        if (_piecesOnPalette.Count == 0)
        {
            for (int i = 0; i < PalettePositions.Length; i++)
            {
                // stwórz wylosowany element
                Piece piece = Instantiate<Piece>(Pieces[Random.Range(0, Pieces.Length)]);
                // obróć i odbij go losowo
                if (piece.Reflectable) piece.transform.Rotate(Random.Range(0, 2) * 180, 0, 0);
                if (piece.Rotable) piece.transform.Rotate(0, Random.Range(0, 4) * 90, 0);
                //if (piece.Reflectable) piece.transform.RotateAround(piece.CenterPoint, Vector3.right, Random.Range(0, 1) * 180);
                //if (piece.Rotable) piece.transform.RotateAround(piece.CenterPoint, Vector3.up, Random.Range(0, 3) * 90);
                // pomniejsz go
                piece.transform.localScale = PaletteScale;
                // przypisz mu miejsce na palecie
                //piece.CalculateCenterPoint();
                piece.PalettePosition = PalettePositions[i].transform.position;
                //piece.transform.position = piece.PalettePosition;
                piece.SetCentralPosition(piece.PalettePosition);
                // dodaj go do listy
                _piecesOnPalette.Add(piece);
            }
            //Random.Range(0,1)
        }
    }

    // Generate new TargetBlock if curremt is null (nullified in pathfinding method);

    // Methods
    //private List<Block> _getNeighbours(Block block)
    //{
    //    List<Block> neighbours = new List<Block>();

    //    if (block.transform.position.z < Board.BoardHeight - 1 && _placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z + 1] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z + 1]);
    //    if (block.transform.position.x < Board.BoardWidth - 1 && _placedBlocksMap[(int)block.transform.position.x + 1, (int)block.transform.position.z] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x + 1, (int)block.transform.position.z]);
    //    if (block.transform.position.z > 0 && _placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z - 1] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z - 1]);
    //    if (block.transform.position.x > 0 && _placedBlocksMap[(int)block.transform.position.x - 1, (int)block.transform.position.z] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x - 1, (int)block.transform.position.z]);

    //    return neighbours;
    //}

    private Block[] _getNeighbours(Block block)
    {
        List<Block> neighbours = new List<Block>();
        /*
        if (block.transform.position.z < Board.BoardHeight - 1 && _placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z + 1] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z + 1]);
        if (block.transform.position.x < Board.BoardWidth - 1 && _placedBlocksMap[(int)block.transform.position.x + 1, (int)block.transform.position.z] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x + 1, (int)block.transform.position.z]);
        if (block.transform.position.z > 0 && _placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z - 1] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x, (int)block.transform.position.z - 1]);
        if (block.transform.position.x > 0 && _placedBlocksMap[(int)block.transform.position.x - 1, (int)block.transform.position.z] != null) neighbours.Add(_placedBlocksMap[(int)block.transform.position.x - 1, (int)block.transform.position.z]);
        */

        Vector3[] directions = new Vector3[4] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        Ray ray = new Ray();
        ray.origin = block.transform.position;
        for (int i = 0; i < directions.Length; i++)
        {
            ray.direction = directions[i];
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, 1) == true)
            {
                neighbours.Add(raycastHit.collider.gameObject.GetComponent<Block>());
                //Debug.Log(raycastHit.transform.position);
            }
        }

        return neighbours.ToArray();
    }

    private bool _findPath(Block block, out List<Block> path)
    {
        path = new List<Block>();
        List<Block> openList = new List<Block>();
        Dictionary<Block, PathfindingInfo> pathfindingInfos = new Dictionary<Block, PathfindingInfo>();
        Block[] neighbours;
        Block current; // = _startingBlock;

        openList.Add(_startingBlock);
        pathfindingInfos[_startingBlock] = new PathfindingInfo();
        Debug.Log("1---");
        //tests:
        int iterations = 0;

        while (true)
        {
            //tests:
            iterations++;
            Debug.Log("Pathfinding iteration: " + iterations);

            openList.Sort(delegate (Block x, Block y)
            {
                return pathfindingInfos[x].StepValue - pathfindingInfos[y].StepValue;
            });

            current = openList[0];
            openList.RemoveAt(0);
            pathfindingInfos[current].State = PathfindingState.Closed;
            //additional rounding: current.transform.position = new Vector3(Mathf.Round(current.transform.position.x), current.transform.position.y, Mathf.Round(current.transform.position.z));
 //           Debug.Log("Current block position: " + current.transform.position.x + ", " + current.transform.position.z);

            if (current == _targetBlock)
            {
                path.Add(_targetBlock);
                for (PathfindingInfo step = pathfindingInfos[_targetBlock]; step.Parent != null; step = pathfindingInfos[step.Parent])
                {
                    path.Add(step.Parent);
                }
                path.Reverse();
                return true;
            }
            Debug.Log("2---");
            neighbours = _getNeighbours(current);
            /*test*/ Debug.Log("neighbours.Length: " + neighbours.Length);
            /*test*/ /*Debug.Log("neighbours[0]: " + pathfindingInfos.ContainsKey(neighbours[0]));*/
            Debug.Log("3---");
            for (int i = 0; i < neighbours.Length; i++)
            {
//              Debug.Log("Neighbour " + i + ": (" + neighbours[i].transform.position.x + ", " + neighbours[i].transform.position.z);
                if (pathfindingInfos.ContainsKey(neighbours[i]) && pathfindingInfos[neighbours[i]].State == PathfindingState.Closed)
                {
                    continue;
                }
                else if (pathfindingInfos.ContainsKey(neighbours[i]) == false || pathfindingInfos.ContainsKey(neighbours[i]) == false && pathfindingInfos[current].CurrentDistanceFromStart < pathfindingInfos[neighbours[i]].CurrentDistanceFromStart)
                {
                    if(pathfindingInfos.ContainsKey(neighbours[i]) == false)
                    {
                        Debug.Log("?");
                        pathfindingInfos[neighbours[i]] = new PathfindingInfo();
                        //pathfindingInfos[neighbours[i]].StraightDistanceFromEnd = Mathf.Abs((int) current.transform.position.x - (int) neighbours[i].transform.position.x) + Mathf.Abs((int) current.transform.position.z - (int) neighbours[i].transform.position.z);
                        pathfindingInfos[neighbours[i]].StraightDistanceFromEnd = Mathf.Abs((int) _targetBlock.transform.position.x - (int) neighbours[i].transform.position.x) + Mathf.Abs((int) _targetBlock.transform.position.z - (int) neighbours[i].transform.position.z);
                        openList.Add(neighbours[i]);
                    }
                    pathfindingInfos[neighbours[i]].CurrentDistanceFromStart = pathfindingInfos[current].CurrentDistanceFromStart + 1;
                    pathfindingInfos[neighbours[i]].Parent = current;
                }
                
            }
            Debug.Log("end---");
            if (openList.Count == 0)
            {
                return false;
            }
        }

    }

    private void _placeStartingBlock()
    {
        GameObject randomBoardspace = _boardspaces[Random.Range(0, _boardspaces.Count)];
        Vector3 newPosition = new Vector3(randomBoardspace.transform.position.x, TargetBlock.transform.localScale.y / 2 + 0.1F, randomBoardspace.transform.position.z);
        GameObject startingBlock = Instantiate<GameObject>(TargetBlock, newPosition, TargetBlock.transform.rotation);
        _startingBlock = startingBlock.GetComponent<Block>();
        _placedBlocksMap[(int) _startingBlock.transform.position.x, (int) _startingBlock.transform.position.z] = _startingBlock;
        newPosition.y = Sphere.transform.position.y;
        _sphere = Instantiate<GameObject>(Sphere, newPosition, Sphere.transform.rotation);
    }

    private void _placeTargetBlock()
    {
        List<GameObject> filteredBoardspaces = new List<GameObject>();

        Ray ray = new Ray();
        ray.direction = Vector3.up;

        for (int i = 0; i < _boardspaces.Count; i++)
        {
            ray.origin =_boardspaces[i].transform.position;
            if (Physics.Raycast(ray, 1) == false)
            {
                filteredBoardspaces.Add(_boardspaces[i]);
            } //else {
                //Debug.Log(_boardspaces[i].transform.position);
            //}
        }

        Vector3[] directions = new Vector3[4] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        ray.origin = new Vector3(_startingBlock.transform.position.x, _boardspaces[0].transform.position.y, _startingBlock.transform.position.z);
        for (int i = 0; i < directions.Length; i++)
        {
            ray.direction = directions[i];
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, 1) == true)
            {
                filteredBoardspaces.Remove(raycastHit.collider.gameObject);
                //Debug.Log(raycastHit.transform.position);
            }
        }

        GameObject randomBoardspace = filteredBoardspaces[Random.Range(0, filteredBoardspaces.Count)];
        Vector3 newPosition = new Vector3(randomBoardspace.transform.position.x, TargetBlock.transform.localScale.y / 2 + 0.1F, randomBoardspace.transform.position.z);
        _targetBlock = Instantiate<GameObject>(TargetBlock, newPosition, Quaternion.identity).GetComponent<Block>();
        _placedBlocksMap[(int)_targetBlock.transform.position.x, (int)_targetBlock.transform.position.z] = _targetBlock;
        //_newTargetBlockGenerated = true;
    }

}
