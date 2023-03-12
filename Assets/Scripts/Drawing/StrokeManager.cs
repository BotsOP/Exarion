using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Drawing
{
    public class StrokeManager : MonoBehaviour, IDataPersistence
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Material drawingMat;
        [SerializeField] private Material displayMat;
        [SerializeField] private int imageWidth;
        [SerializeField] private int imageHeight;
        [SerializeField] private int brushSize;
    
        [SerializeField] private int brushStrokeIDToRedraw;
        [SerializeField] private float brushStartTime;
        [SerializeField] private float brushEndTime;
        public Transform ball1;
        public Transform ball2;
    
        private RenderTexture drawingRenderTexture;
        private int kernelID;
        private Vector2 threadGroupSizeOut;
        private Vector2 threadGroupSize;
        private Vector2 lastCursorPos;
        private bool firstUse = true;

        private int currentID = 1;
        private int lastID;
        private float strokeStartTime;
        private float cachedTime;
        private Vector4 tempBox;
        private Drawing drawer;

        private float time => Time.time / 10;

        void OnEnable()
        {
            drawer = new Drawing(imageWidth, imageHeight);

            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);

            tempBox = ResetTempBox(tempBox);
        }

        void Update()
        {
            if(Input.GetMouseButton(0))
            {
                RaycastHit hit;
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    Vector2 cursorPos = new Vector2(hit.point.x * imageWidth, hit.point.y * imageHeight);

                    if (firstUse)
                    {
                        lastID = currentID;
                        strokeStartTime = time;
                        lastCursorPos = cursorPos;
                        firstUse = false;
                    }

                    bool firstStroke = lastID == currentID;

                    drawer.DrawStroke(lastCursorPos, cursorPos, brushSize, cachedTime, time, firstStroke, lastID);

                    lastCursorPos = cursorPos;
                    currentID++;
                
                    if (imageWidth > cursorPos.x) { tempBox.x = cursorPos.x; }
                    if (imageHeight > cursorPos.y) { tempBox.y = cursorPos.y; }
                    if (0 < cursorPos.x) { tempBox.z = cursorPos.x; }
                    if (0 < cursorPos.y) { tempBox.w = cursorPos.y; }
                    ball1.position = new Vector3(tempBox.x / imageWidth, tempBox.y / imageHeight, 0);
                    ball2.position = new Vector3(tempBox.z / imageWidth, tempBox.w / imageHeight, 0);
                }
            }
            else
            {
                //runs once after mouse is not being clicked anymore
                if (!firstUse)
                {
                    int strokeID = drawer.GetRandomID();
                    Debug.Log(strokeID);
                    drawer.brushStrokesID.Add(new BrushStrokeID(lastID, currentID, strokeStartTime, time, tempBox));
                    tempBox = ResetTempBox(tempBox);
                    firstUse = true;
                }
            }

            cachedTime = time;

            if (Input.GetKeyDown(KeyCode.B))
            {
                drawer.Redraw(brushStrokeIDToRedraw, brushStartTime, brushEndTime);
            }
        }

        private Vector4 ResetTempBox(Vector4 box)
        {
            box.x = imageWidth;
            box.y = imageHeight;
            box.z = 0;
            box.w = 0;
            return box;
        }

        private bool CheckCollision(Vector4 box1, Vector4 box2)
        {
            return box1.x <= box2.z && box1.z >= box2.x && box1.y <= box2.w && box1.w >= box2.y;
        }

        public void LoadData(ToolData data)
        {
            currentID = data.currentID;
            drawer.brushStrokes = data.brushStrokes;
            drawer.brushStrokesID = data.brushStrokesID;
            drawer.RedrawAll();
        }

        public void SaveData(ToolData data)
        {
            data.brushStrokes = drawer.brushStrokes;
            data.brushStrokesID = drawer.brushStrokesID;
            data.currentID = currentID;
        }
    }

    public static class ExtensionMethods {
        public static float Remap (this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}