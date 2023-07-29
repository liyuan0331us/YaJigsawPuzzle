using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum PieceState { Normal,Corner,CornerRight}
public class PicPiece : MonoBehaviour
{
    public Transform[] pieceArr;//0,1,2分别是凹、凸、平面
    public Texture[] lightMap0Arr;
    public Texture[] lightMap1Arr;
    public Texture[] lightMap2Arr;

    [HideInInspector]
    public int[] dirNotch;

    [HideInInspector]
    public Vector3 origPos;

    public List<Vector2Int> containIndicesArr;
    public Vector2Int mainIndices;

    public PieceState pieceState = PieceState.Normal;

    [HideInInspector]
    public SortingGroup sorting;
    //Start is called before the first frame update
    void Awake()
    {
        containIndicesArr = new List<Vector2Int>();
        sorting = GetComponent<SortingGroup>();
        //SetUp(3, Vector3.zero, new int[] { 0, 1, 2, 2 });
    }

    public void SetCornerRight()
    {
        if (pieceState != PieceState.Corner) return;
        sorting.sortingOrder = short.MinValue;
        pieceState = PieceState.CornerRight;
    }

    public void CombinePiece(PicPiece targetPiece)
    {
        if (targetPiece == null || targetPiece == this) return;

        if (targetPiece.pieceState == PieceState.Normal)
        {
            CombinePieceNojudge(targetPiece);
        }
        else
        {
            targetPiece.CombinePieceNojudge(this);
        }
    }
    public void CombinePieceNojudge(PicPiece targetPiece)
    {
        //print("合并");
        for (int i = targetPiece.transform.childCount - 1; i >= 0; i--) 
        {
            targetPiece.transform.GetChild(i).parent = transform;
        }

        targetPiece.pieceState = PieceState.CornerRight;
        GameObject.Destroy(targetPiece.gameObject);
    }

    public void SetUp(int gridCount, Vector2Int indices, float scale, Vector2 origPos, int[] dirNotch, int sortCount)//dirNotch传递长度为4的数组，分别表示上、右、下、左
    {
        if (dirNotch.Length != 4) {
            Debug.LogWarning("必须传递长度为4的数组，分别表示上、右、下、左");
            return;
        }
        
        this.origPos = origPos;
        this.dirNotch = dirNotch;
        sorting.sortingOrder = sortCount;
        mainIndices = indices;
        containIndicesArr.Add(indices);
        if (containIndicesArr.Contains(new Vector2Int(0, 0))
            || containIndicesArr.Contains(new Vector2Int(gridCount - 1, 0))
            || containIndicesArr.Contains(new Vector2Int(0, gridCount - 1))
            || containIndicesArr.Contains(new Vector2Int(gridCount - 1, gridCount - 1))
            )
        {
            pieceState = PieceState.Corner;
        }

        transform.position = Vector3.zero;
        Transform childTrans = transform.GetChild(0);

        childTrans.localScale = Vector3.one * scale;
        childTrans.position = origPos;
        childTrans.name = "" + indices.ToString();

        int zAngle = 0;
        Transform tempTrans;
        for (int i = 0; i < 4; i++)
        {
            tempTrans = GameObject.Instantiate<Transform>(pieceArr[dirNotch[i]], childTrans);
            tempTrans.localPosition = Vector3.zero;
            tempTrans.rotation = Quaternion.Euler(0, 180, zAngle);

            //Light贴图与反转
            Material tempMat = tempTrans.GetComponent<MeshRenderer>().material;
            switch (dirNotch[i])
            {
                case 0://凹下
                    if (zAngle == 90 || zAngle == 180)
                    {
                        tempMat.SetTexture("_AddTex", lightMap0Arr[1]);
                    }
                    if (zAngle == 180 || zAngle == 270)
                    {
                        tempMat.SetInt("_LightFlip", 1);
                    }
                    break;
                case 1://突起
                    if (zAngle == 90 || zAngle == 180)
                    {
                        tempMat.SetTexture("_AddTex", lightMap1Arr[1]);
                    }
                    if (zAngle == 180 || zAngle == 270)
                    {
                        tempMat.SetInt("_LightFlip", 1);
                    }
                    break;
                case 2://平面
                    if (zAngle == 90 || zAngle == 180)
                    {
                        tempMat.SetTexture("_AddTex", lightMap2Arr[1]);
                    }
                    if (zAngle == 180 || zAngle == 270)
                    {
                        tempMat.SetInt("_LightFlip", 1);
                    }
                    break;
            }


            //设置UV2
            Mesh mesh = tempTrans.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];
            //Matrix4x4 localToWorld = tempTrans.localToWorldMatrix;
            for (int j = 0; j < uvs.Length; j++)
            {
                Vector3 worldVertice = tempTrans.TransformPoint(vertices[j]);
                //Vector3 worldVertice = localToWorld.MultiplyPoint3x4(vertices[j]);
                //print("~~~~~~");
                //print(worldVertice);
                //print(vertices[j]);
                float remapU = worldVertice.x.Remap(-5, 5, 0, 1);
                float remapV = worldVertice.y.Remap(-5, 5, 0, 1);
                uvs[j] = new Vector2(remapU, remapV);
            }
            mesh.uv2 = uvs;

            //方向旋转
            zAngle += 90;
        }
    }
}

//public struct PieceInfo
//{
//    public Vector2Int indices;
//    public Vector2 position;
//}

public static class ExtensionMethods
{

    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

}