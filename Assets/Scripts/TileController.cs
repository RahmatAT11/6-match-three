using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public int id;

    private BoardManager board;
    private SpriteRenderer render;

    private static readonly Color selectedColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color normalColor = Color.white;

    private static readonly float moveDuration = 0.5f;

    private static readonly Vector2[] adjacentDirection = 
    {
        Vector2.up, Vector2.down,
        Vector2.left, Vector2.right,
    };
    
    private static TileController previousSelected = null;

    private bool isSelected = false;
    
    public bool IsDestroyed { get; private set; }

    private void Awake()
    {
        board = BoardManager.Instance;
        render = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        // Non selectable conditions
        if (render.sprite == null || board.IsAnimating)
        {
            return;
        }
        
        // apakah sudah memilih tile ini?
        if (isSelected)
        {
            Deselect();
        }
        else
        {
            // jika belum ada yang terpilih
            if (previousSelected == null)
            {
                Select();
            }
            else
            {
                // apakah ada tile ini bertetanggaan?
                if (GetAllAdjacentTiles().Contains(previousSelected))
                {
                    TileController otherTile = previousSelected;
                    previousSelected.Deselect();
                    
                    // swap tile
                    SwapTile(otherTile, () =>
                    {
                        if (board.GetAllMatches().Count > 0)
                        {
                            Debug.Log("MATCH FOUND");
                        }
                        else
                        {
                            SwapTile(otherTile);
                        }
                    });
                }
                // jika tidak bertetanggaan maka ubah tile yang dipilih
                else
                {
                    previousSelected.Deselect();
                    Select();
                }
            }
        }
    }

    public void SwapTile(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(board.SwapTilePosition(this, otherTile, onCompleted));
    }

    public void ChangeId(int id, int x, int y)
    {
        render.sprite = board.tileTypes[id];
        this.id = id;

        name = "TILE_" + id + "(" + x + "," + y + ")";
    }

    #region Adjacent

    private TileController GetAdjecent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.size.x);

        if (hit)
        {
            return hit.collider.GetComponent<TileController>();
        }

        return null;
    }

    public List<TileController> GetAllAdjacentTiles()
    {
        List<TileController> adjacentTiles = new List<TileController>();

        for (int i = 0; i < adjacentDirection.Length; i++)
        {
            adjacentTiles.Add(GetAdjecent(adjacentDirection[i]));
        }

        return adjacentTiles;
    }

    #endregion

    #region Check Match

    private List<TileController> GetMatch(Vector2 castDir)
    {
        List<TileController> matchingTiles = new List<TileController>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir,
            render.size.x);

        while (hit)
        {
            TileController otherTile = hit.collider.GetComponent<TileController>();
            if (otherTile.id != id || otherTile.IsDestroyed)
            {
                break;
            }
            
            matchingTiles.Add(otherTile);
            hit = Physics2D.Raycast(otherTile.transform.position, castDir,
                render.size.x);
        }

        return matchingTiles;
    }

    private List<TileController> GetOneLineMatch(Vector2[] paths)
    {
        List<TileController> matchingTiles = new List<TileController>();

        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(GetMatch(paths[i]));
        }
        
        // cocokkan jika lebih dari 2 tile (3 dengan dirinya) dalam satu baris
        if (matchingTiles.Count >= 2)
        {
            return matchingTiles;
        }

        return null;
    }

    public List<TileController> GetAllMatches()
    {
        if (IsDestroyed)
        {
            return null;
        }

        List<TileController> matchingTiles = new List<TileController>();
        
        // ambil pasangan-pasangan untuk arah horizontal dan vertikal
        List<TileController> horizontalMatchingTiles = GetOneLineMatch(new Vector2[2]
            {Vector2.up, Vector2.down});
        List<TileController> verticalMatchingTiles = GetOneLineMatch(new Vector2[2]
            {Vector2.left, Vector2.right});

        if (horizontalMatchingTiles != null)
        {
            matchingTiles.AddRange(horizontalMatchingTiles);
        }

        if (verticalMatchingTiles != null)
        {
            matchingTiles.AddRange(verticalMatchingTiles);
        }
        
        // tambahkan tile ini ke tile-tile yang sudah berpasangan jika ketemu pasangannya
        if (matchingTiles != null && matchingTiles.Count >= 2)
        {
            matchingTiles.Add(this);
        }

        return matchingTiles;
    }

    #endregion

    #region Select & Deselect

    private void Select()
    {
        isSelected = true;
        render.color = selectedColor;
        previousSelected = this;
    }

    private void Deselect()
    {
        isSelected = false;
        render.color = normalColor;
        previousSelected = null;
    }

    #endregion

    public IEnumerator MoveTilePosition(Vector2 targetPosition, System.Action onCompleted)
    {
        Vector2 startPosition = transform.position;
        float time = 0.0f;
        
        // menjalankan animasi di frame selanjutnya untuk alasan keamanan
        yield return new WaitForEndOfFrame();

        while (time < moveDuration)
        {
            transform.position = Vector2.Lerp(
                startPosition, targetPosition, time / moveDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.position = targetPosition;
        
        onCompleted?.Invoke();  
    }
}
