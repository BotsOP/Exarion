using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
 
//Code by Albarnie
 
[InitializeOnLoad]
public class FolderSelectionHelper
{
    //The last object we had selected
    public static Object lastActiveObject;
    //Object to select the next frame
    public static Object objectToSelect;
    //Whether we have locked the inspector
    public static bool hasLocked;
 
    static FolderSelectionHelper()
    {
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update += OnUpdate;
 
        //Restore folder lock status
        hasLocked = EditorPrefs.GetBool("FolderSelectionLocked", false);
    }
 
    static void OnSelectionChanged()
    {
        if (lastActiveObject != null && Selection.activeObject != null)
        {
            //If the selection has actually changed
            if (Selection.activeObject != lastActiveObject)
            {
                //If the new object is a folder, reselect our old object
                if (IsAssetAFolder(Selection.activeObject))
                {
                    //We have to select the object the next frame, otherwise it will not register
                    objectToSelect = lastActiveObject;
                }
                else
                {
                    UnLockFolders();
                    //Update the last object
                    lastActiveObject = Selection.activeObject;
                }
            }
        }
        else if (!IsAssetAFolder(Selection.activeObject))
        {
            lastActiveObject = Selection.activeObject;
            UnLockFolders();
        }
 
    }
 
    //We have to do selecting in the next editor update because Unity does not allow selecting another object in the same editor update
    static void OnUpdate()
    {
        //If the editor is locked then we don't care
        if (objectToSelect != null && !ActiveEditorTracker.sharedTracker.isLocked)
        {
            //Select the new object
            Selection.activeObject = objectToSelect;
 
            LockFolders();
 
            lastActiveObject = objectToSelect;
            objectToSelect = null;
        }
        else
        {
            objectToSelect = null;
        }
    }
 
    static void LockFolders()
    {
        ActiveEditorTracker.sharedTracker.isLocked = true;
        hasLocked = true;
        //We store the state so that if we compile or leave the editor while the folders are locked then the state is kept
        EditorPrefs.SetBool("FolderSelectionLocked", true);
    }
 
    static void UnLockFolders()
    {
        //Only unlock inspector if we are the one who locked it
        if (hasLocked)
        {
            ActiveEditorTracker.sharedTracker.isLocked = false;
            hasLocked = false;
            EditorPrefs.SetBool("FolderSelectionLocked", false);
        }
    }
 
    private static bool IsAssetAFolder(Object obj)
    {
        string path = "";
 
        if (obj == null)
        {
            return false;
        }
 
        //Get the path to the asset
        path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
 
        //If the asset is a directory (i.e a folder)
        if (path.Length > 0 && Directory.Exists(path))
        {
            return true;
        }
 
        return false;
    }
 
}