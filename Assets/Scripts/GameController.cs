using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using PathFind = NesScripts.Controls.PathFind;

public class GameController : MonoBehaviour
{
    public Board Board;
    public Block TargetBlock;
    public GameObject Sphere;
    public float SphereSpeed;
    public GameObject[] PalettePositions;
    public Vector3 PaletteScale;
    public Piece[] Pieces;
    public Material HighlightMaterial;

    private GameObject SelectedPiece = null;
    private Block StartBlock;
    private Block EndBlock;
    private Block[,] BlockMap;
    private float[,] ValueMap;
    private Transform[,] BoardSpaceMap;
    private Vector3 InputOffset;
    private float InputZ;
    private List<Transform> TilesBelow;
    private Material DefaultMaterial;
    private List<PathFind.Point> Path;
    private Vector3 WayPoint;
    private int WayPointIndex = 0;
    private int PaletteItems = 0;
    bool SphereMoving = false;

    #region Game Start

    void Start()
    {
        InitializeBoard();
        InitializePalette();
        PlaceTarget();
    }


    void InitializeBoard()
    {
        BlockMap = new Block[Board.BoardWidth, Board.BoardHeight];
        ValueMap = new float[Board.BoardWidth, Board.BoardHeight];
        for (int i = 0; i < Board.BoardWidth; i++)
            for (int j = 0; j < Board.BoardHeight; j++)
                ValueMap[i, j] = 0.0f;

        BoardSpaceMap = new Transform[Board.BoardWidth, Board.BoardHeight];
        int x = 0;
        int y = 0;
        for (int i = 0; i < Board.transform.childCount; i++)
        {
            if (x > Board.BoardWidth - 1)
            {
                x = 0;
                y++;
            }
            BoardSpaceMap[x, y] = Board.transform.GetChild(i);
            BoardSpaceMap[x, y].GetComponent<BoardSpace>().x = x;
            BoardSpaceMap[x, y].GetComponent<BoardSpace>().y = y;

            x++;
        }

        // reuse x && y to spawn starting block
        x = Random.Range(0, Board.BoardWidth);
        y = Random.Range(0, Board.BoardHeight);
        StartBlock = Instantiate(TargetBlock, BoardSpaceMap[x, y].position, Quaternion.identity);
        BlockMap[x, y] = StartBlock;
        ValueMap[x, y] = 1.0f;
        StartBlock.x = x;
        StartBlock.y = y;
        // spawn sphere
        Sphere = Instantiate(Sphere, BoardSpaceMap[x, y].position, Quaternion.identity);
        Sphere.transform.Translate(0, 1, 0);
        Sphere.GetComponent<SphereController>().x = x;
        Sphere.GetComponent<SphereController>().y = y;
        // set default material
        DefaultMaterial = BoardSpaceMap[0, 0].GetComponent<MeshRenderer>().material;

        Path = new List<PathFind.Point>();
        WayPoint = Sphere.transform.position;
    }

    void InitializePalette()
    {
        for (int i = 0; i < PalettePositions.Length; i++)
        {
            Piece piece = Instantiate<Piece>(Pieces[Random.Range(0, Pieces.Length)]);
            if (piece.Reflectable) piece.transform.Rotate(Random.Range(0, 2) * 180, 0, 0);
            if (piece.Rotable) piece.transform.Rotate(0, Random.Range(0, 4) * 90, 0);
            piece.transform.localScale = PaletteScale;
            piece.PalettePosition = PalettePositions[i].transform.position;
            piece.SetCentralPosition(piece.PalettePosition);
        }
        PaletteItems = PalettePositions.Length;
    }

    void PlaceTarget()
    {
        int x = Random.Range(0, Board.BoardWidth);
        int y = Random.Range(0, Board.BoardHeight);

        if (BlockMap[x, y] == null)
        {
            EndBlock = Instantiate(TargetBlock, BoardSpaceMap[x, y].position, Quaternion.identity);
            BlockMap[x, y] = EndBlock;
            ValueMap[x, y] = 1.0f;
            EndBlock.x = x;
            EndBlock.y = y;
        }
        else
            PlaceTarget();
    }

    #endregion

    #region Game Update

    void Update()
    {
        SelectPiece();
        MovePiece();
        PlacePiece();
        UpdateScore();
        if (PaletteItems == 0)
            InitializePalette();
    }

    private void FixedUpdate()
    {
        MoveSphere();
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = InputZ;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void SelectPiece()
    {
        if (Input.GetMouseButtonDown(0) && !SphereMoving)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.tag == "Block")
                {
                    SelectedPiece = hit.collider.gameObject.transform.parent.gameObject;
                    InputZ = Camera.main.WorldToScreenPoint(SelectedPiece.transform.position).z;
                    InputOffset = SelectedPiece.transform.position - GetMouseAsWorldPoint();
                }
            }
        }
    }

    private void GetTilesBelow()
    {
        TilesBelow = new List<Transform>();
        if (SelectedPiece != null)
        {
            Transform zeroChild = SelectedPiece.transform.GetChild(0);
            Vector3 roundedPosition = new Vector3
            {
                x = Mathf.Round(zeroChild.position.x),
                y = Mathf.Round(zeroChild.position.y),
                z = Mathf.Round(zeroChild.position.z)
            };

            int layerMask = 1 << 8;
            RaycastHit hit;

            if (Physics.Raycast(roundedPosition, Vector3.down, out hit, Mathf.Infinity, layerMask))
            {
                Debug.DrawRay(roundedPosition, Vector3.down * hit.distance, Color.yellow);

                int startX = hit.collider.transform.GetComponent<BoardSpace>().x;
                int startY = hit.collider.transform.GetComponent<BoardSpace>().y;

                for (int i = 0; i < SelectedPiece.transform.childCount; i++)
                {
                    Transform child = SelectedPiece.transform.GetChild(i);

                    int offsetX = (int)child.transform.localPosition.x;
                    int offsetY = (int)child.transform.localPosition.z;

                    bool flipX = false, flipY = false, replace = false, replaceFlipped = false;

                    int rotationY = (int)SelectedPiece.transform.rotation.eulerAngles.y;
                    int rotationZ = (int)SelectedPiece.transform.rotation.eulerAngles.z;
                    rotationY = (int)(Mathf.Round(rotationY / 90) * 90);
                    rotationZ = (int)(Mathf.Round(rotationZ / 90) * 90);

                    if (rotationY == 0)
                    {
                        if (rotationZ == 180)
                            flipX = true;
                    }
                    else if (rotationY == 90)
                    {
                        replace = true;
                        if (rotationZ != 180)
                            flipX = true;
                    }
                    else if (rotationY == 180)
                    {
                        flipY = true;
                        if (rotationZ != 180)
                            flipX = true;
                    }
                    else if (rotationY == 270)
                    {
                        replaceFlipped = true;
                        if (rotationZ != 180)
                            flipX = true;
                    }

                    if (flipX)
                        offsetX *= -1;
                    if (flipY)
                        offsetY *= -1;
                    if (replace)
                    {
                        int b = offsetX;
                        offsetX = offsetY;
                        offsetY = b;
                    }
                    else if (replaceFlipped)
                    {
                        int b = -offsetY;
                        offsetY = -offsetX;
                        offsetX = b;
                    }

                    if (startX + offsetX >= 0 && startX + offsetX < Board.BoardWidth)
                        if (startY + offsetY >= 0 && startY + offsetY < Board.BoardHeight)
                            TilesBelow.Add(BoardSpaceMap[startX + offsetX, startY + offsetY]);
                }
            }
            else
            {
                Debug.DrawRay(roundedPosition, Vector3.down * 1000, Color.white);
            }
        }
    }
    
    private void RevertTiles()
    {
        for (int i = 0; i < Board.BoardWidth; i++)
        {
            for (int j = 0; j < Board.BoardHeight; j++)
            {
                BoardSpaceMap[i, j].GetComponent<MeshRenderer>().material = DefaultMaterial;
            }
        }
    }

    private void HighlightTiles()
    {
        RevertTiles();
        for (int i = 0; i < TilesBelow.Count; i ++)
        {
            int x = TilesBelow[i].GetComponent<BoardSpace>().x;
            int y = TilesBelow[i].GetComponent<BoardSpace>().y;
            BoardSpaceMap[x, y].GetComponent<MeshRenderer>().material = HighlightMaterial;
        }
    }

    private void MovePiece()
    {
        if (SelectedPiece != null)
        {
            SelectedPiece.transform.position = GetMouseAsWorldPoint() + InputOffset;
            GetTilesBelow();
            HighlightTiles();
        }
    }

    private void PlacePiece()
    {
        if (!Input.GetMouseButton(0) && SelectedPiece != null)
        {
            bool placeable = true;

            for (int i = 0; i < TilesBelow.Count; i++)
            {
                int x = TilesBelow[i].GetComponent<BoardSpace>().x;
                int y = TilesBelow[i].GetComponent<BoardSpace>().y;
                if (BlockMap[x, y] != null)
                {
                    placeable = false;
                    break;
                }
            }

            if (TilesBelow.Count == SelectedPiece.transform.childCount && placeable)
            {
                SelectedPiece.transform.localScale = Vector3.one;
                for (int i = 0; i < TilesBelow.Count; i++)
                {
                    int x = TilesBelow[i].GetComponent<BoardSpace>().x;
                    int y = TilesBelow[i].GetComponent<BoardSpace>().y;
                    Block block = SelectedPiece.transform.GetChild(i).GetComponent<Block>();
                    block.transform.position = TilesBelow[i].transform.position;
                    BlockMap[x, y] = block;
                    ValueMap[x, y] = 10.0f;
                }
                PaletteItems--;
                FindPath();
            }
            else
            {
                Piece piece = SelectedPiece.GetComponent<Piece>();
                piece.SetCentralPosition(piece.PalettePosition);
            }
            SelectedPiece = null;
            TilesBelow = new List<Transform>();
            RevertTiles();
        }
    }

    private void FindPath()
    {
        if (Path.Count == 0)
        {
            PathFind.Grid grid = new PathFind.Grid(ValueMap);
            PathFind.Point _from = new PathFind.Point(StartBlock.x, StartBlock.y);
            PathFind.Point _to = new PathFind.Point(EndBlock.x, EndBlock.y);
            Path = PathFind.Pathfinding.FindPath(grid, _from, _to, PathFind.Pathfinding.DistanceType.Manhattan);
            GetNextWayPoint();
        }
    }

    private void GetNextWayPoint()
    {
        if (WayPointIndex < Path.Count)
        {
            WayPoint = BoardSpaceMap[Path[WayPointIndex].x, Path[WayPointIndex].y].transform.position;
            WayPoint.y = Sphere.transform.position.y;
            WayPointIndex++;
        }
        else if (WayPointIndex == Path.Count && Path.Count != 0)
        {
            Sphere.transform.position.Set(EndBlock.transform.position.x, Sphere.transform.position.y, EndBlock.transform.position.z);
            WayPoint = Sphere.transform.position;
            WayPointIndex = 0;

            ValueMap[StartBlock.x, StartBlock.y] = 0.0f;
            Destroy(StartBlock.gameObject);
            StartBlock = EndBlock;
            PlaceTarget();

            Path = new List<PathFind.Point>();
            FindPath();
        }
    }

    private void MoveSphere()
    {
        if (WayPoint != Sphere.transform.position)
        {
            Vector3 direction = WayPoint - Sphere.transform.position;
            Sphere.transform.Translate(direction.normalized * SphereSpeed * Time.deltaTime, Space.World);
            if (Vector3.Distance(Sphere.transform.position, WayPoint) <= 0.03f)
            {
                int layerMask = 1 << 8;
                RaycastHit hit;
                Vector3 roundedPosition = new Vector3
                {
                    x = Mathf.Round(Sphere.transform.position.x),
                    y = Mathf.Round(Sphere.transform.position.y),
                    z = Mathf.Round(Sphere.transform.position.z)
                };

                if (Physics.Raycast(roundedPosition, Vector3.down, out hit, Mathf.Infinity, layerMask))
                {
                    int x = hit.collider.transform.GetComponent<BoardSpace>().x;
                    int y = hit.collider.transform.GetComponent<BoardSpace>().y;
                    if (x != EndBlock.x || y != EndBlock.y)
                    {
                        ValueMap[x, y] = 0.0f;
                        Destroy(BlockMap[x, y].gameObject);
                        BlockMap[x, y] = null;
                    }
                }
                GetNextWayPoint();
            }
            SphereMoving = true;
        }
        else
            SphereMoving = false;
    }

    private void UpdateScore()
    {
        
    }
    #endregion
}
