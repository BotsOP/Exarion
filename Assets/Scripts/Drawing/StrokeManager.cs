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
    
        [SerializeField] private PaintType paintType;
        public Transform ball1;
        public Transform ball2;
        public Drawing drawer;
    
        private RenderTexture drawingRenderTexture;
        private int kernelID;
        private Vector2 threadGroupSizeOut;
        private Vector2 threadGroupSize;
        private Vector2 lastCursorPos;
        private bool firstUse = true;

        private float brushSize;
        private int brushStrokeID;
        private float cachedTime;
        private Vector4 collisionBox;
        private CommandManager commandManager;

        private float time => (Time.time / 10) % 1;

        void OnEnable()
        {
            drawer = new Drawing(imageWidth, imageHeight);
            commandManager = FindObjectOfType<CommandManager>();

            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);

            ResetTempBox(out collisionBox);
            
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            EventSystem.Subscribe(EventType.STOPPED_DRAWING, StoppedDrawing);
            EventSystem<int, float, float>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
        }

        private void OnDisable()
        {
            EventSystem<Vector2>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem.Unsubscribe(EventType.STOPPED_DRAWING, StoppedDrawing);
            EventSystem<int, float, float>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<float>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
        }


        private void SetBrushSize(float brushSize)
        {
            this.brushSize = brushSize;
        }

        private void Draw(Vector2 mousePos)
        {
            bool firstDraw = firstUse;

            if (lastCursorPos == mousePos)
            {
                return;
            }

            if (firstUse)
            {
                brushStrokeID = drawer.GetNewID();
                lastCursorPos = mousePos;
                firstUse = false;
            }

            drawer.Draw(lastCursorPos, mousePos, brushSize, paintType, cachedTime, time, firstDraw, brushStrokeID);
            drawer.AddBrushDraw(new BrushStroke(lastCursorPos, mousePos, brushSize, time, cachedTime));

            lastCursorPos = mousePos;
                
            if (collisionBox.x > mousePos.x) { collisionBox.x = mousePos.x; }
            if (collisionBox.y > mousePos.y) { collisionBox.y = mousePos.y; }
            if (collisionBox.z < mousePos.x) { collisionBox.z = mousePos.x; }
            if (collisionBox.w < mousePos.y) { collisionBox.w = mousePos.y; }
            ball1.position = new Vector3(collisionBox.x / imageWidth, collisionBox.y / imageHeight, 0);
            ball2.position = new Vector3(collisionBox.z / imageWidth, collisionBox.w / imageHeight, 0);
        }
        
        private void StoppedDrawing()
        {
            drawer.FinishedStroke(collisionBox, paintType);
            ICommand draw = new DrawCommand(ref drawer, collisionBox, paintType);
            commandManager.Execute(draw, false);
                    
            ResetTempBox(out collisionBox);
            firstUse = true;
        }
        
        private void RedrawStroke(int brushStrokeIDToRedraw, float brushStartTime, float brushEndTime)
        {
            Debug.Log($"test");
            drawer.Redraw(brushStrokeIDToRedraw, brushStartTime, brushEndTime);
        }

        void Update()
        {
            cachedTime = time;
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
            drawer.brushStrokes = data.brushStrokes;
            drawer.brushStrokesID = data.brushStrokesID;
            drawer.RedrawAll();
        }

        public void SaveData(ToolData data)
        {
            data.brushStrokes = drawer.brushStrokes;
            data.brushStrokesID = drawer.brushStrokesID;
        }
    }

    public static class ExtensionMethods {
        public static float Remap (this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}