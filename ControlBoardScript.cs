using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using OscCore;

// This script can be attached to a GameObject (i.e main camera)
public class ControlBoardScript : MonoBehaviour
{
    // Input/Text UI elements
    public InputField superColliderIP;
    public InputField sceneIP;
    public Text debugInfo;
    // Clients from OscCore for SuperCollider and the 3D scene
    public static OscClient superColliderClient;
    // This is private since it will not be used in the other script file
    private OscClient sceneClient;

    // Function to apply new IP address and port for SuperCollider
    public void OnClickSetSuperColliderServer()
    {
        string address = "";
        int port = 0;
        debugInfo.text = "";

        // Validate the input first
        if (ValidateIpAndPort(superColliderIP.text, ref address, ref port))
        {
            superColliderClient = new OscClient(address, port);
            // Send a "hello" message to SuperCollider
            if (superColliderClient != null)
            {
                string msg = "Hello from control board!";
                string addressPattern = "/hello";
                superColliderClient.Send(addressPattern, msg);
                debugInfo.text = "Message '" + msg + "' is sent to SC at "
                + superColliderClient.Destination + addressPattern;
            }
        }
        else
            debugInfo.text = "Invalid input for IP:Port of SC";
    }

    // Function to apply new IP address and port for the 3D scene
    public void OnClickSetSceneServer()
    {
        string address = "";
        int port = 0;
        debugInfo.text = "";

        if (ValidateIpAndPort(sceneIP.text, ref address, ref port))
        {
            sceneClient = new OscClient(address, port);

            // Send a "hello" message to 3D scene
            if (sceneClient != null)
            {
                string msg = "Hello from control board!";
                string addressPattern = "/hello";
                sceneClient.Send(addressPattern, msg);
                debugInfo.text = "Message '" + msg + "' is sent to the 3D scene at " + sceneClient.Destination + addressPattern;
            }
        }
        else
            debugInfo.text = "Invalid input for IP:Port of the 3D scene";
    }

    // Function for when the value of slider x, y or z is changed
    public void OnSliderChange(float value)
    {
        // Which slider is being changed
        string addressPattern = "/" + EventSystem.current.currentSelectedGameObject.name;
        // Check if the address pattern dictionary contains this name as a key
        if (sceneClient != null)
        {
            sceneClient.Send(addressPattern, value);
            debugInfo.text = "Message '" + value + "' is sent to 3D scene at " + sceneClient.Destination + addressPattern;
        }
    }

    // Send a MIDI number to SuperCollider
    public void SendKeyToSuperCollider()
    {
        // Buttons are named as "X" with X as MIDI number from 52 to 63
        string keyName = EventSystem.current.currentSelectedGameObject.name;
        // Parse the name as an integer to send to SC
        int key = System.Int32.Parse(keyName);
        string addressPattern = "/midi";
        // Send to the sub-address "/midi" on the server of SC
        if (superColliderClient != null)
        {
            superColliderClient.Send(addressPattern, key);
            debugInfo.text = "Message '" + key + "' is sent to SC at "
            + superColliderClient.Destination + addressPattern;
        }
    }

    // Function to exit the application i.e. per custom Button-Click)
    public void OnClickQuit()
    {
        FreeAllInSuperCollider();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Overwrite a default Unity function (quit not per custom Button-Click)
    void OnApplicationQuit()
    {
        FreeAllInSuperCollider();
    }

    /* Function to check the syntax validity of given IP:Port
    Adapted from: https://stackoverflow.com/a/21061273 */
    bool ValidateIpAndPort(string input, ref string address, ref int port)
    {
        // First split the input into 2 parts using ":" as the delimiter
        string[] parts = input.Split(':');
        if (parts.Length != 2)
            return false;

        // Split the first part into a further 4 parts using "." as the delimiter
        string[] ip = parts[0].Split('.');
        if (ip.Length != 4)
            return false;

        // Port must be a number between 1 and 65535
        string tempPort = parts[1];
        bool validPort = ValidateNum(tempPort, 1, 65535);

        if (validPort)
        {
            foreach (string item in ip)
            {
                // Each part of the IP must be a number between 0 and 255
                if (!ValidateNum(item, 0, 255))
                    return false;
            }
            // Return the values to the caller through reference
            address = parts[0];
            port = Int32.Parse(tempPort);
            return true;
        }
        else
            return false;

    }

    // Assisted function for "ValidateIpAndPort"
    bool ValidateNum(string input, int min, int max)
    {
        int num = Int32.Parse(input);
        return num >= min && num <= max && input == num.ToString();
    }

    // Send a message to trigger freeing all used resource in SuperCollider
    void FreeAllInSuperCollider()
    {
        if (superColliderClient != null)
            superColliderClient.Send("/stopAll");
    }
}