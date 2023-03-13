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

        private int brushStrokeID;
        private float cachedTime;
        private Vector4 collisionBox;
        private Drawing drawer;
        private CommandManager commandManager;

        private float time => Time.time / 10;

        void OnEnable()
        {
            drawer = new Drawing(imageWidth, imageHeight);
            commandManager = FindObjectOfType<CommandManager>();

            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);

            ResetTempBox(out collisionBox);
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

                    bool firstDraw = firstUse;

                    if (firstUse)
                    {
                        brushStrokeID = drawer.GetNewID();
                        lastCursorPos = cursorPos;
                        firstUse = false;
                    }

                    drawer.Draw(lastCursorPos, cursorPos, brushSize, cachedTime, time, firstDraw, brushStrokeID);
                    drawer.AddBrushDraw(new BrushStroke(lastCursorPos, cursorPos, brushSize, time, cachedTime));

                    lastCursorPos = cursorPos;
                
                    if (collisionBox.x > cursorPos.x) { collisionBox.x = cursorPos.x; }
                    if (collisionBox.y > cursorPos.y) { collisionBox.y = cursorPos.y; }
                    if (collisionBox.z < cursorPos.x) { collisionBox.z = cursorPos.x; }
                    if (collisionBox.w < cursorPos.y) { collisionBox.w = cursorPos.y; }
                    ball1.position = new Vector3(collisionBox.x / imageWidth, collisionBox.y / imageHeight, 0);
                    ball2.position = new Vector3(collisionBox.z / imageWidth, collisionBox.w / imageHeight, 0);
                }
            }
            else
            {
                //runs once after mouse is not being clicked anymore
                if (!firstUse)
                {
                    drawer.FinishedStroke(collisionBox);
                    ICommand draw = new DrawCommand(ref drawer, collisionBox);
                    commandManager.Execute(draw, false);
                    ResetTempBox(out collisionBox);
                    firstUse = true;
                }
            }

            cachedTime = time;

            if (Input.GetKeyDown(KeyCode.B))
            {
                drawer.Redraw(brushStrokeIDToRedraw, brushStartTime, brushEndTime);
            }
        }

        private void ResetTempBox(out Vector4 box)
        {
            box.x = imageWidth;
            box.y = imageHeight;
            box.z = 0;
            box.w = 0;
        }

        private bool CheckCollision(Vector4 box1, Vector4 box2)
        {
            return box1.x <= box2.z && box1.z >= box2.x && box1.y <= box2.w && box1.w >= box2.y;
        }

        public void LoadData(ToolData data)
        {
            //drawer.brushDrawID = data.currentID;
            drawer.BrushStrokes = data.brushStrokes;
            drawer.brushStrokesID = data.brushStrokesID;
            drawer.RedrawAll();
        }

        public void SaveData(ToolData data)
        {
            data.brushStrokes = drawer.BrushStrokes;
            data.brushStrokesID = drawer.brushStrokesID;
            //data.currentID = drawer.brushDrawID;
        }
    }

    public static class ExtensionMethods {
        public static float Remap (this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}