using System;
using System.Collections;
using System.Collections.Generic;
using Undo;
using UnityEngine;

namespace Drawing
{
    public class DrawingManager : MonoBehaviour, IDataPersistence
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Material drawingMat;
        [SerializeField] private Material displayMat;
        public int imageWidth = 2048;
        public int imageHeight = 2048;
    
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
        private float startBrushStrokeTime;
        private Vector4 collisionBox;
        private Vector4 resetBox;
        private CommandManager commandManager;

        private float time;

        void OnEnable()
        {
            drawer = new Drawing(imageWidth, imageHeight);
            commandManager = FindObjectOfType<CommandManager>();

            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);

            resetBox = new Vector4(imageWidth, imageHeight, 0, 0);
            collisionBox = resetBox;
            
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            EventSystem<float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<float>.Subscribe(EventType.TIME_SHOWCASE, SetShowcaseTime);
            EventSystem<int, float, float>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);

        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem<Vector2>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<float>.Unsubscribe(EventType.TIME_SHOWCASE, SetShowcaseTime);
            EventSystem<int, float, float>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
        }

        private void SetTime(float time)
        {
            this.time = time;
        }
        private void SetShowcaseTime(float time)
        {
            displayMat.SetFloat("_TimeSpeed", time);
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
                startBrushStrokeTime = time;
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
            // ball1.position = new Vector3(collisionBox.x / imageWidth, collisionBox.y / imageHeight, 0);
            // ball2.position = new Vector3(collisionBox.z / imageWidth, collisionBox.w / imageHeight, 0);
        }
        
        private void StoppedDrawing()
        {
            drawer.FinishedStroke(collisionBox, paintType, startBrushStrokeTime, time);
            EventSystem<int, float, float>.RaiseEvent(EventType.FINISHED_STROKE, drawer.brushStrokesID.Count - 1, startBrushStrokeTime, time);
            ICommand draw = new DrawCommand(ref drawer, collisionBox, paintType,drawer.brushStrokesID.Count - 1, startBrushStrokeTime, time);
            commandManager.Execute(draw);

            collisionBox = resetBox;
            firstUse = true;
        }
        
        private void RedrawStroke(int brushStrokeIDToRedraw, float brushStartTime, float brushEndTime)
        {
            drawer.Redraw(brushStrokeIDToRedraw, brushStartTime, brushEndTime);
        }

        void Update()
        {
            cachedTime = time;
        }

        public void LoadData(ToolData data)
        {
            drawer.brushStrokes = data.brushStrokes;
            drawer.brushStrokesID = data.brushStrokesID;
            drawer.RedrawAll();
            drawer.RedrawAllTimelineClips();
        }

        public void SaveData(ToolData data)
        {
            data.brushStrokes = drawer.brushStrokes;
            data.brushStrokesID = drawer.brushStrokesID;
        }
    }

    
}