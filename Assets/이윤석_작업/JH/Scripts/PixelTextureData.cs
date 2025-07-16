using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelTextureData
{
    public TerrainLayer layer;
    public Vector2Int coord ;

    public PixelTextureData(TerrainLayer l,int x, int z)
    {
        coord = new Vector2Int(x,z);
        layer = l;
    }

}