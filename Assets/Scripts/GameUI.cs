using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameUI : MonoBehaviour
{
    public Texture samplePicture;

    public PicPiece prefabPicPiece;
    public Material[] baseMaterials;
    // Start is called before the first frame update
    void Start()
    {
        StartGame(4, samplePicture);
    }

    short curMaxSortCount;
    int gridCount;
    PicPiece[,] picPieceArr2;
    float scale;
    public void StartGame(int gridCount, Texture picture)
    {
        foreach (var mat in baseMaterials)
        {
            mat.SetTexture("_Picture", picture);
        }

        curMaxSortCount = short.MinValue;
        this.gridCount = gridCount;

        picPieceArr2 = new PicPiece[gridCount, gridCount];
        scale = 10f / gridCount;

        float dx, dy;
        dx = dy = scale;

        PicPiece tempPiece;
        for (int i = 0; i < gridCount; i++)
        {
            for (int j = 0; j < gridCount; j++)
            {
                tempPiece = GameObject.Instantiate<PicPiece>(prefabPicPiece);

                //创建dirNotch
                int[] dirNotch = new int[4] { 0, 0, 0, 0 };
                //上
                if (j == gridCount - 1) dirNotch[0] = 2;
                else
                {
                    dirNotch[0] = Random.Range(0, 2);
                }
                //右
                if (i == gridCount - 1) dirNotch[1] = 2;
                else
                {
                    dirNotch[1] = Random.Range(0, 2);
                }
                //下
                if (j == 0) dirNotch[2] = 2;
                else
                {
                    dirNotch[2] = picPieceArr2[i, j - 1].dirNotch[0] == 0 ? 1 : 0;
                }
                //左
                if (i == 0) dirNotch[3] = 2;
                else
                {
                    dirNotch[3] = picPieceArr2[i - 1, j].dirNotch[1] == 0 ? 1 : 0;
                }

                tempPiece.SetUp(gridCount, new Vector2Int(i, j), scale, Indices2Pos(new Vector2Int(i, j)), dirNotch, curMaxSortCount);
                tempPiece.transform.name = i + "," + j;
                curMaxSortCount += 2;
                picPieceArr2[i, j] = tempPiece;
            }
        }

        //拼图打散
        foreach (var forPiece in picPieceArr2)
        {
            Vector2 pos;
            while (true) {
                float val1 = Random.Range(-8f - scale * .5f, 8f + scale * .5f);
                float val2 = Random.Range(-8f - scale * .5f, 8f + scale * .5f);
                
                if(!(Mathf.Abs(val1)<5.5f+scale * .5f && Mathf.Abs(val2) < 5.5f+scale * .5f))
                {
                    pos = new Vector2(val1, val2);
                    break;
                }
            }

            forPiece.transform.position = pos - (Vector2)forPiece.origPos;
        }

    }

    [HideInInspector]
    public PicPiece dragingObj = null;
    Vector3 dragOffset;
    Ray ray;
    RaycastHit[] hitArr;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())//点击到UI
            {
                //Debug.Log("Clicked on the UI");
                return;
            }

            dragingObj = null;
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            hitArr = Physics.RaycastAll(ray, 100, 1 << 11);
            RaycastHit? targetHit = null;
            foreach (var curHit in hitArr)
            {
                if (curHit.transform.GetComponentInParent<PicPiece>().pieceState == PieceState.CornerRight) continue;
                if (targetHit == null) targetHit = curHit;
                else
                {
                    PicPiece targetPiece = targetHit.Value.transform.GetComponentInParent<PicPiece>();
                    PicPiece curPiece = curHit.transform.GetComponentInParent<PicPiece>();
                    if (curPiece.sorting.sortingOrder > targetPiece.sorting.sortingOrder) targetHit = curHit;
                }
            }
            if (targetHit.HasValue)
            {
                //print("hit");
                dragingObj = targetHit.Value.transform.GetComponentInParent<PicPiece>();
                dragOffset = dragingObj.transform.position - targetHit.Value.point;

                dragingObj.sorting.sortingOrder = curMaxSortCount;
                curMaxSortCount += 2;
                //print(dragingObj);
            }
        }
        if (Input.GetMouseButton(0) && dragingObj)
        {
            MoveDragingObj();
        }
        if (Input.GetMouseButtonUp(0) && dragingObj)
        {
            DropPicec();
            //Vector3 pos = dragingObj.transform.position;
            //pos.z = 0;
            //dragingObj.transform.position = pos;
            //dragingObj = null;
        }
    }
    void MoveDragingObj()
    {
        if (!dragingObj) return;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, 1 << 10))
        {
            Vector3 targetPos = hit.point + dragOffset - ray.direction * 2.5f;
            dragingObj.transform.position = targetPos;
        }
    }

    void JudgeWin()
    {
        //print(FindObjectsOfType<PicPiece>().Length);
        //if (FindObjectsOfType<PicPiece>().Length <= 4) print("win");
        int count = 0;
        foreach (var forPiece in picPieceArr2)
        {
            if (forPiece == null) continue;
            //print(forPiece.name);
            if (forPiece.pieceState != PieceState.CornerRight) count++;
        }
        //print(count);
        if (count == 0) print("win!");
    }

    const float fitPercent = .15f;
    void DropPicec()
    {
        if (!dragingObj) return;
        Vector3 pos = dragingObj.transform.position;
        pos.z = 0;

        if (dragingObj.pieceState == PieceState.Corner
            && Vector2.Distance(dragingObj.transform.position, Vector2.zero) < scale * fitPercent)
        {
            pos = Vector2.zero;
            dragingObj.transform.position = pos;
            dragingObj.SetCornerRight();
        }
        else
        {
            foreach (var forPiece in picPieceArr2)
            {
                if (forPiece == null || forPiece == dragingObj) continue;

                if (Vector2.Distance(dragingObj.transform.position, forPiece.transform.position) < scale * fitPercent)
                {
                    //pos = Vector2.zero;
                    for (int i = 0; i < dragingObj.transform.childCount; i++)
                    {
                        for (int j = 0; j < forPiece.transform.childCount; j++)
                        {
                            Transform transformI = dragingObj.transform.GetChild(i);
                            Transform transformJ = forPiece.transform.GetChild(j);
                            if (Vector2.Distance(transformI.position, transformJ.position) < scale * (1 + fitPercent))
                            {
                                pos = forPiece.transform.position;
                                dragingObj.transform.position = pos;
                                forPiece.CombinePiece(dragingObj);
                                break;
                            }
                        }
                    }
                }
            }
        }


        //dragingObj.transform.position = pos;
        JudgeWin();
        dragingObj = null;
    }

    //PicPiece GetPieceWithIndices(Vector2Int indices)
    //{
    //    foreach(var curPiece in picPieceArr2)
    //    {
    //        if (curPiece == null) continue;
    //        foreach (var curIndices in curPiece.containIndicesArr)
    //        {
    //            if (curIndices == indices) return curPiece;
    //        }
    //    }

    //    return null;
    //}

    Vector2 Indices2Pos(Vector2Int indices)
    {
        float dx, dy;
        dx = dy = 10f / gridCount;

        Vector2 result = new Vector2(dx * (indices.x + .5f), dy * (indices.y + .5f)) - Vector2.one * 5f;
        //result.x /= -1f;
        return new Vector2(dx * (indices.x + .5f), dy * (indices.y + .5f)) - Vector2.one * 5f;
    }

    //Vector2Int[] GetAllNearIndices(PicPiece picPiece)
    //{
    //    List<Vector2Int> result = new List<Vector2Int>();

    //    foreach(var indices in picPiece.containIndicesArr)
    //    {
    //        Vector2Int newIndices;
    //        if (indices.x > 0)
    //        {
    //            newIndices = indices + Vector2Int.left;
    //            //print(1);
    //            if (!picPiece.containIndicesArr.Contains(newIndices))
    //                result.Add(newIndices);
    //        }
    //        if (indices.x < gridCount - 1)
    //        {
    //            newIndices = indices + Vector2Int.right;
    //            //print(2);
    //            if (!picPiece.containIndicesArr.Contains(newIndices))
    //                result.Add(newIndices);
    //        }
    //        if (indices.y > 0)
    //        {
    //            newIndices = indices + Vector2Int.down;
    //            //print(3);
    //            if (!picPiece.containIndicesArr.Contains(newIndices))
    //                result.Add(newIndices);
    //        }
    //        if(indices.y < gridCount - 1)
    //        {
    //            newIndices = indices + Vector2Int.up;
    //            //print(4);
    //            if (!picPiece.containIndicesArr.Contains(newIndices))
    //                result.Add(newIndices);
    //        }
    //    }

    //    return result.ToArray();
    //}

    public struct Range
    {
        public float min;
        public float max;
        public float range { get { return max - min + 1; } }
        public Range(float aMin, float aMax)
        {
            min = aMin; max = aMax;
        }
    }

    public static float RandomValueFromRanges(params Range[] ranges)
    {
        if (ranges.Length == 0)
            return 0;
        float count = 0;
        foreach (Range r in ranges)
            count += r.range;
        float sel = Random.Range(0, count);
        foreach (Range r in ranges)
        {
            if (sel < r.range)
            {
                return r.min + sel;
            }
            sel -= r.range;
        }
        return 0;
    }
}
