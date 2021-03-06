﻿/*
 * Launchpad Macaques - Neon Oblivion
 * Zackary Seiple
 * NarrativeTriggerEditor.cs
 * This script handles the custom inspector for NarrativeTriggerHandler.cs, adds some buttons for easier access as well as
 * hides certain items from a trigger if they aren't relevant
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NarrativeTriggerHandler)), CanEditMultipleObjects]
public class NarrativeTriggerEditor : Editor
{
    NarrativeTriggerHandler narrativeTriggerHandler;
    //The foldout that contains every trigger
    bool triggerMainFoldout = true;

    private void OnEnable()
    {
        narrativeTriggerHandler = (NarrativeTriggerHandler)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //Random Trigger Settings (Universal)
        EditorGUILayout.LabelField("Global Trigger Settings", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Random Trigger Settings", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomIntervalMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomIntervalMax"));
        EditorGUILayout.LabelField("Player Hitting Ground Settings",EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fallTime"));
        if (serializedObject.FindProperty("randomIntervalMin").floatValue > serializedObject.FindProperty("randomIntervalMax").floatValue)
        {
            EditorGUILayout.HelpBox("randomIntervalMax must be bigger than or equal to randomIntervalMin", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        SerializedProperty array = serializedObject.FindProperty("triggers");
        SerializedProperty arraySub = serializedObject.FindProperty("triggerSubFoldout");
        SerializedProperty arrayNames = serializedObject.FindProperty("triggerNames");

        if(array.arraySize > 0)
            triggerMainFoldout = EditorGUILayout.Foldout(triggerMainFoldout, "Triggers", true);
        //If the main foldout is out and all triggers are being shown
        if (triggerMainFoldout)
        {
            EditorGUI.indentLevel++;
            //Iterate through and display each trigger in a foldout
            for (int i = 0; i < array.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                arraySub.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.Foldout(arraySub.GetArrayElementAtIndex(i).boolValue, arrayNames.GetArrayElementAtIndex(i).stringValue);
                if(GUILayout.Button("Delete Trigger"))
                {
                    if(narrativeTriggerHandler.triggers[i].areaTrigger != null)
                    {
                        DestroyImmediate(narrativeTriggerHandler.triggers[i].areaTrigger);
                    }

                    array.DeleteArrayElementAtIndex(i);

                    arrayNames.DeleteArrayElementAtIndex(i);
                    //triggerNames.RemoveAt(i);
                    arraySub.DeleteArrayElementAtIndex(i);
                    //triggerSubFoldout.RemoveAt(i);
                    serializedObject.ApplyModifiedProperties();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                //If the subfoldout should be out, display all the details for this trigger
                if (arraySub.GetArrayElementAtIndex(i).boolValue)
                {

                    SerializedProperty element = array.GetArrayElementAtIndex(i);
                    EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("type"));
                    //Time in level triggers should NEVER be repeatable as it only occurs once
                    if(!((element.FindPropertyRelative("type").enumValueIndex == (int)NarrativeTriggerHandler.TriggerType.OnEvent) &&
                       (element.FindPropertyRelative("eventType").enumValueIndex == (int)NarrativeTriggerHandler.EventType.TimeInLevel)))
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("repeatable"));

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Text Options", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Text to Display", GUILayout.Width(120));
                    narrativeTriggerHandler.triggers[i].textToDisplay = EditorGUILayout.TextArea(narrativeTriggerHandler.triggers[i].textToDisplay, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("textDisplayTime"));

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Audio Options", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("audioToPlay"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("audioSource"));

                   


                    int type = element.FindPropertyRelative("type").enumValueIndex;
                    //Display Area Trigger specific options
                    if (type == (int)NarrativeTriggerHandler.TriggerType.Area)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Area Trigger Options", EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Triggering Tag");
                        element.FindPropertyRelative("triggeringTag").stringValue = EditorGUILayout.TagField(element.FindPropertyRelative("triggeringTag").stringValue);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Spawn Trigger Zone"))
                        {
                            GameObject newAreaTrigger = new GameObject("Area Trigger");
                            newAreaTrigger.transform.localScale = new Vector3(5, 5, 5);
                            element.FindPropertyRelative("areaTrigger").objectReferenceValue = newAreaTrigger;
                            serializedObject.ApplyModifiedProperties();
                            Selection.activeGameObject = newAreaTrigger;
                            SceneView.FrameLastActiveSceneView();
                            Selection.activeGameObject = narrativeTriggerHandler.gameObject;
                        }
                        if (narrativeTriggerHandler.triggers[i].areaTrigger != null)
                        {
                            if (GUILayout.Button("Go to Trigger"))
                            {
                                Selection.activeGameObject = narrativeTriggerHandler.triggers[i].areaTrigger;
                                SceneView.FrameLastActiveSceneView();
                                Selection.activeGameObject = narrativeTriggerHandler.gameObject;

                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("areaCenter"));
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("boxSize"));
                    }
                    //Display onEvent Trigger specific options
                    else if (type == (int)NarrativeTriggerHandler.TriggerType.OnEvent)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Event Trigger Options", EditorStyles.boldLabel);
                        SerializedProperty eventType = element.FindPropertyRelative("eventType");
                        EditorGUILayout.PropertyField(eventType);
                        //Time in Level
                        if (eventType.enumValueIndex == 1)
                        {
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("timeInLevelBeforeTrigger"));
                        }
                        //LookAtObject
                        else if(eventType.enumValueIndex == 2)
                        {
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("triggeringObjects"));
                        }
                    }

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Trigger"))
        {
            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            //triggerSubFoldout.Add(new bool());
            arraySub.InsertArrayElementAtIndex(index);

            arrayNames.InsertArrayElementAtIndex(index);
            arrayNames.GetArrayElementAtIndex(index).stringValue = "Trigger " + (index + 1);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(narrativeTriggerHandler);
    }

    private void OnSceneGUI()
    {
        Handles.color = Color.cyan;

        //For area triggers, display a wireframe cube in the scene that represents the trigger area
        for(int i = 0; i < narrativeTriggerHandler.triggers.Length; i++)
        {
            GameObject currentAreaTrigger;
            if((currentAreaTrigger = narrativeTriggerHandler.triggers[i].areaTrigger) != null)
            {
                EditorGUI.BeginChangeCheck();

                Handles.DrawWireCube(currentAreaTrigger.transform.position, currentAreaTrigger.transform.localScale);
                Vector3 positionforLabel = currentAreaTrigger.transform.position;
                positionforLabel.y += currentAreaTrigger.transform.localScale.y / 2;
                Handles.Label(positionforLabel, narrativeTriggerHandler.GetTriggerName(i));
                Vector3 position =  Handles.PositionHandle(currentAreaTrigger.transform.position, Quaternion.identity);
                Vector3 scale = Handles.ScaleHandle(currentAreaTrigger.transform.localScale, currentAreaTrigger.transform.position, Quaternion.identity, 10);
                Quaternion rotation = Handles.RotationHandle(currentAreaTrigger.transform.localRotation, currentAreaTrigger.transform.position);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changed Trigger Properties");
                    currentAreaTrigger.transform.position = narrativeTriggerHandler.triggers[i].areaCenter = position;
                    currentAreaTrigger.transform.localScale = narrativeTriggerHandler.triggers[i].boxSize = scale;
                    currentAreaTrigger.transform.localRotation = rotation;
                }
            }

           
        }
        
    }
}
