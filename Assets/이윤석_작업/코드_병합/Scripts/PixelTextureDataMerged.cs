using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelTextureDataMerged
{
    public TerrainLayer layer;
    public Vector2Int coord ;

    public PixelTextureDataMerged(TerrainLayer l,int x, int z)
    {
        coord = new Vector2Int(x,z);
        layer = l;
    }

}