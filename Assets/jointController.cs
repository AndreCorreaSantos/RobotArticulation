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

    public Joint joint1;
    public Joint joint2;

    private List<Joint> joints;


    private float rotationSpeed = 45;
    public float learningRate = 0.1f;

    public bool cpu = true;

    public Transform target;

    public Transform endEffector;

    public RenderTexture renderTexture;

    void Start() {
        // Initialize the RenderTexture
        joints = new List<Joint> { joint1, joint2 };
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
        if (cpu){
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

        if (Input.GetKeyDown(KeyCode.Space)) {
            ShaderInverseKinematics();
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

        float j1_length = Vector3.Distance(joint1.jointObject.transform.position, joint2.jointObject.transform.position);
        float j2_length = Vector3.Distance(joint2.jointObject.transform.position, endEffector.position);

        int kernelIndex = gradientShader.FindKernel("CSMain");

        gradientShader.SetVector("target", target.position);
        gradientShader.SetFloat("j1_length", j1_length);
        gradientShader.SetFloat("j2_length", j2_length);
        gradientShader.SetVector("base_position", joint1.jointObject.transform.position);

        Debug.Log("target position: " + target.position);
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


}

