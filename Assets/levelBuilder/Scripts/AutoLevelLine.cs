using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

/* Esta clase nos permitira instanciar los prefabs del auto level 

estos prefabs pueden ser: 
- Un solo prefab de muro estirado
- un prefab de puerta en el centro y dos muros a los lados?
- un prefab repetido varias veces para llenar el espacio. 

 */

[SelectionBase]
public class AutoLevelLine : MonoBehaviour{

    [Header("Line Info")]
    public int fromVertexIndex;
    public int toVertextIndex;

    public AutoLevelPrefabPack prefabs;
    public Vector3 startPos;
    public Vector3 endPos;
    public float distance;
    public Vector3 direction;
    public Vector3 center;

    [HideInInspector]
    public int TypeIndex_ =1; //this is the Prefab we will be using to generate the line
    public int TypeIndex{
        get{return TypeIndex_;}
        set{
            TypeIndex_ = value;
            GenerateGeometryNew();
            WriteToMesh();
        }
    }

    public AutoLevel autoLevel;

    public AutoLevel.LevelBuilderInfo info;

    public void setup(Vector3 start, Vector3 end, int fromIndex, int toIndex ,  AutoLevel autoLevel_){

        prefabs = autoLevel_.prefabs;
        autoLevel = autoLevel_;
        
        startPos = start;
        endPos = end;
        direction = end - startPos;
        center = (startPos + endPos) / 2;
        //center += transform.up *  (prefabs.FloorHeight / 2);
        distance = Vector3.Distance(startPos, endPos);

        transform.position = center;
        transform.rotation = Quaternion.LookRotation(direction);

        fromVertexIndex = fromIndex;
        toVertextIndex = toIndex;

        //can we get the type info from the mesh color? 
        Color fromColor = autoLevel.colors[fromIndex];

        TypeIndex_ = (int)fromColor.r;

        //for each line that is setup we want to get the ID of the level info saved in the mesh
        var infoId = autoLevel.colors[fromIndex].b;
        //a number less than 10 we will create a new INFO object on the autoLevel
        if(infoId>10){
            var info_ = autoLevel.info.Find(x => x.uid == infoId);
            Debug.Log("Loaded Info with ID: " + infoId.ToString());
            if(info_.uid != 0){
                info = info_;
            }else{
                CreateInfo();
            }
        }
        else{
            CreateInfo();
        }

        GenerateGeometryNew();

        transform.localScale = new Vector3(info.flip?-1:1, 1, 1);
    }

    public void CreateInfo(){
        info  = new AutoLevel.LevelBuilderInfo {
                uid = autoLevel.info.Count()+11,
                info = null,
                flip = false
        };
        autoLevel.info.Add(info);
        autoLevel.SaveLevelInfoId(fromVertexIndex, info.uid);
    }

    public void UpdateInfoObject(int id, object o){
        Debug.Log("Updating Info Object"+ id.ToString() + " " + o.ToString());
        var infoUpdated = autoLevel.info.Find(x => x.uid == id);
        infoUpdated.info = o;
        autoLevel.info.Remove(autoLevel.info.Find(x => x.uid == info.uid));
        autoLevel.info.Add(infoUpdated);
    }

    public void GenerateGeometryNew(){
        //delete all children
        while(transform.childCount > 0){
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if(TypeIndex_ == 0){
            return;
        }

        var linePrefab = prefabs.LinePrefabs[TypeIndex_];

        switch(linePrefab.alignment){
            case PrefabAlignment.Stretch:
                GenerateStretch(linePrefab);
                break;
            case PrefabAlignment.Center:
                GenerateCenter(linePrefab);
                break;
            case PrefabAlignment.Repeat:
                GenerateRepeated(linePrefab);
                break;
        }
    }

    //creates one single child and streches it the full width of the line
    public void GenerateStretch(LinePrefab linePrefab){
        #if UNITY_EDITOR
        
        GameObject newObject = PrefabUtility.InstantiatePrefab(linePrefab.prefab) as GameObject;
        newObject.transform.position = center;
        newObject.transform.rotation = Quaternion.LookRotation(direction);
        newObject.transform.localScale = new Vector3(newObject.transform.localScale.x, newObject.transform.localScale.y, distance/linePrefab.width);
        newObject.transform.SetParent(transform);
        
        #endif    
    }

    //creates one child at the center point and spawns to walls at the sides
    public void GenerateCenter(LinePrefab linePrefab){
        #if UNITY_EDITOR

        GameObject objectAtCenter = PrefabUtility.InstantiatePrefab(linePrefab.prefab) as GameObject;
        objectAtCenter.transform.position = center;
        objectAtCenter.transform.rotation = Quaternion.LookRotation(direction);        
        objectAtCenter.transform.SetParent(transform);

        //if the istantiated object implements this interface we want to get the level builder info it might need.
        var levelBuilderInfoGetter = objectAtCenter.GetComponent<IlevelBuilderInfoGetter>();
        if(levelBuilderInfoGetter != null){
            levelBuilderInfoGetter.GetLevelBuilderInfo(info, this);
        }

        //we are not always creating a door but well.
        var doorWidth = linePrefab.width;
        //next we want to create two walls
        //one from the start position to the center - the width/2
        CreateAndStretch(prefabs.LinePrefabs[linePrefab.sidePrefabIndex], startPos, center - (transform.forward * doorWidth/2));
        //one from the center position to the end position - the width/2
        CreateAndStretch(prefabs.LinePrefabs[linePrefab.sidePrefabIndex], center + (transform.forward * doorWidth/2), endPos);
       

        #endif
    }

    //creates multiple children to fill the width of the line
    public void GenerateRepeated(LinePrefab linePrefab){
        #if UNITY_EDITOR

        //calculate the number of windows we can fit in the space
        var windowWidth = linePrefab.width;
        var rotationOffset = linePrefab.offsetRotation;
        var windowCount = Mathf.FloorToInt(distance / windowWidth);
        var remainingSpace = distance - (windowCount * windowWidth);
        var offset = remainingSpace / windowCount;
        
        for(int i = 0; i < windowCount; i++){
            var windowPosition = startPos + transform.forward * (windowWidth * i);
            windowPosition += transform.forward * (windowWidth/1.5f);
            windowPosition += transform.forward * (offset/2);
            //windowPosition += transform.up * (prefabs.FloorHeight / 2);

            GameObject window = PrefabUtility.InstantiatePrefab(linePrefab.prefab) as GameObject;
            window.transform.position = windowPosition;
            window.transform.rotation = Quaternion.LookRotation(direction);

            window.transform.SetParent(transform);
            window.transform.localScale = new Vector3(window.transform.localScale.x, window.transform.localScale.y, window.transform.localScale.z+offset);
            window.transform.Rotate(rotationOffset, Space.Self);
        }

        #endif
    }

    //this functions takes the current type of the line and saves it in the mesh
    public void WriteToMesh(){
        autoLevel?.SaveLineType(fromVertexIndex,TypeIndex);
    }


    void CreateAndStretch(LinePrefab linePrefab, Vector3 start, Vector3 end){
        #if UNITY_EDITOR

        var center_ = (start + end) / 2;
        //center_ += transform.up * (prefabs.FloorHeight / 4);
        var distance_ = Vector3.Distance(start, end);

        GameObject newObject = PrefabUtility.InstantiatePrefab(linePrefab.prefab) as GameObject;
        newObject.transform.position = center_;
        newObject.transform.rotation = Quaternion.LookRotation(direction);

        newObject.transform.localScale = new Vector3(newObject.transform.localScale.x, newObject.transform.localScale.y, distance_/linePrefab.width);
        newObject.transform.SetParent(transform);
        
        #endif
    }

    public void Flip(){
        var newX = transform.localScale.x*-1;
        transform.localScale = new Vector3(newX, 1, 1);
        info.flip = !info.flip;
        //modify the info in the auto level list
        autoLevel.info.Remove(autoLevel.info.Find(x => x.uid == info.uid));
        autoLevel.info.Add(info);
    }

    public void saveInfoId(int newId){
        autoLevel?.SaveLevelInfoId(fromVertexIndex, newId);
    }

    //draw a gizmo to visualize the line 
    void OnDrawGizmos(){
        Gizmos.color = Color.blue;
        Matrix4x4 originalMatrix = Gizmos.matrix;
        // Set the Gizmos matrix to the object's local transform matrix/space
        Gizmos.matrix = transform.localToWorldMatrix;
        // Draw the cube using local coordinates
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        // Restore the original Gizmos matrix
        Gizmos.matrix = originalMatrix;
    }
}