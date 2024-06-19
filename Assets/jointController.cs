using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class Joint{
    public string jointAxis;
    public GameObject jointObject;
    public Vector3 rotationAxis = Vector3.up; // Default to rotating around the Y-axis
}

public class jointController : MonoBehaviour
{
    public ComputeShader gradientShader;
    public Joint[] joints;
    private float rotationSpeed = 45;
    public float learningRate = 0.1f;

    public bool keyBoardInput = false;

    public Transform target;

    public Transform endEffector;

    public RenderTexture renderTexture;

    void Start() {
        // Initialize the RenderTexture
        InitializeRenderTexture();
        ShaderInverseKinematics();
    }

    void InitializeRenderTexture() {
        renderTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }



    // Update is called once per frame
    void Update()
    {
        if (keyBoardInput){
            foreach (Joint joint in joints){ 

                float input = Input.GetAxis(joint.jointAxis); // forward kinematics
                if(Mathf.Abs(input) > 0){
                    float degrees = 1;
                    updateAngle(degrees,joint,input);
                }

            }
        }else{
            inverseKinematics();
        }
    }




    void updateAngle(float degrees, Joint joint, float input){
        float rotationAmount = input * degrees;
        Quaternion rotation = Quaternion.Euler(joint.rotationAxis * rotationAmount);
        joint.jointObject.transform.rotation *= rotation;
    }


    public void inverseKinematics() {
        Vector3 targetPosition = target.position;
        float learningRate = 0.1f;
        int maxIterations = 200;

        for (int i = 0; i < maxIterations; i++) {
            Vector3 error = targetPosition - endEffector.position;

            if (error.magnitude < 0.001f) break; // Close enough

            foreach (Joint joint in joints) {
                float derivative = ErrorDerivative(joint, error, 0.01f);
                joint.jointObject.transform.Rotate(joint.rotationAxis, -learningRate * derivative);
            }
        }
    }

    float ErrorDerivative(Joint joint, Vector3 error, float deltaAngle) {
        // store original rotation
        Quaternion originalRotation = joint.jointObject.transform.rotation;

        joint.jointObject.transform.Rotate(joint.rotationAxis, deltaAngle);
        Vector3 newPos = endEffector.position;
        Vector3 newError = target.position - newPos;

        // restore original rotation
        joint.jointObject.transform.rotation = originalRotation;

        // approximate derivative
        return (newError.magnitude - error.magnitude) / deltaAngle;
    }


    public void ClearRenderTexture(RenderTexture renderTexture, Color clearColor)
    {
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, clearColor);  
        RenderTexture.active = null;     
    }

    public void ShaderInverseKinematics() {
        ClearRenderTexture(renderTexture, Color.clear);

        int kernelIndex = gradientShader.FindKernel("CSMain");

        gradientShader.SetVector("target", target.position);
        gradientShader.SetTexture(kernelIndex, "Result", renderTexture);
        gradientShader.Dispatch(kernelIndex, renderTexture.width / 8, renderTexture.height / 8, 1);
        SaveRenderTextureToPNG();
    }

    private void SaveRenderTextureToPNG() {
        // Ensure the RenderTexture is active
        RenderTexture.active = renderTexture;

        // Create a new Texture2D to read the data
        Texture2D outputTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        outputTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        outputTexture.Apply();

        // Encode the texture to a PNG
        byte[] bytes = outputTexture.EncodeToPNG();

        // Write to a file
        string filePath = Path.Combine(Application.dataPath, "SavedScreenshots");
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        filePath = Path.Combine(filePath, "RenderedImage.png");
        File.WriteAllBytes(filePath, bytes);

        // Log the location
        Debug.Log($"Saved RenderTexture to PNG at {filePath}");

        // Clean up
        RenderTexture.active = null;
        Destroy(outputTexture);
    }

// private IEnumerator DrawPlane() {
//     // Dispatch a compute shader to randomly sample points in the configuration space and calculate their gradient

//     int kernelHandle = gradientShader.FindKernel("CSMain");
//     RenderTexture tex = new RenderTexture(256, 256, 24);
//     tex.enableRandomWrite = true;
//     tex.Create();

//     // Set the texture to the compute shader
//     gradientShader.SetTexture(kernelHandle, "Result", tex);
//     gradientShader.SetVector("targetPosition", target.position);
//     gradientShader.SetVector("endEffectorPosition", endEffector.position);
//     gradientShader.SetFloat("learningRate", 0.1f);
    
//     // Dispatch the compute shader
//     int threadGroupsX = Mathf.CeilToInt(tex.width / 8f);
//     int threadGroupsY = Mathf.CeilToInt(tex.height / 8f);
//     gradientShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

//     // Wait for the shader to finish
//     yield return new WaitForEndOfFrame();

//     // Read the texture back to a Texture2D
//     Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
//     RenderTexture.active = tex;
//     tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
//     tex2D.Apply();

//     // Save the texture as a PNG file
//     byte[] bytes = tex2D.EncodeToPNG();
//     string filePath = Application.dataPath + "/../gradient.png";
//     System.IO.File.WriteAllBytes(filePath, bytes);
//     Debug.Log("Gradient image saved to: " + filePath);

//     // Clean up
//     tex.Release();
//     RenderTexture.active = null;

//     yield return null;
// }



}

