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

    public bool keyboard = true;

    public Transform target;

    public Transform endEffector;

    private RenderTexture renderTexture;

    private RenderTexture debugTexture;

    public GameObject gradientPlane;
    public bool shader = false;

    public Material gradientDisplacementMaterial;

    void Start() {
        // Initialize the RenderTexture
        joints = new List<Joint> { joint1, joint2 };
        InitializeRenderTexture();
        InitializeDebugTexture();
        ShaderInverseKinematics();
    }

    void InitializeRenderTexture() {
        renderTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        // Set the RenderTexture as the main texture of the gradientPlane
        gradientDisplacementMaterial.SetTexture("_MainTex", renderTexture);
    }

    void InitializeDebugTexture() {
        debugTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        debugTexture.enableRandomWrite = true;
        debugTexture.Create();
    }



    // Update is called once per frame
    void Update()
    {
        if (keyboard){
            foreach (Joint joint in joints){ 
                float input = Input.GetAxis(joint.jointAxis); // forward kinematics
                if(Mathf.Abs(input) > 0){
                    float degrees = 1;
                    updateAngle(degrees,joint,input);
                }
            }
        }else if(shader){
            // Debug.Log("Shader");
            ShaderInverseKinematics();
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

        Vector2 tpos = new Vector2(target.position.x, target.position.y);
        Vector2 j1pos = joint1.jointObject.transform.position;
        Vector2 j2pos = joint2.jointObject.transform.position;
        Vector2 base_position = joint1.jointObject.transform.position;
        Vector2 end_position = endEffector.position;

        float j1_length = Vector2.Distance(j1pos, j2pos);
        float j2_length = Vector2.Distance(j2pos, end_position);

        int kernelIndex = gradientShader.FindKernel("CSMain");

        Debug.Log($"Target Position, before setting: {tpos}");
        gradientShader.SetVector("target", tpos);
        gradientShader.SetFloat("j1_length", j1_length);
        gradientShader.SetFloat("j2_length", j2_length);
        gradientShader.SetVector("base_position", base_position);


        gradientShader.SetTexture(kernelIndex, "Result", renderTexture);
        gradientShader.SetTexture(kernelIndex, "Debug", debugTexture);

        gradientShader.Dispatch(kernelIndex, renderTexture.width / 8, renderTexture.height / 8, 1);

        // Read back debug and find the best angles
        RenderTexture.active = debugTexture;
        Texture2D readTexture = new Texture2D(debugTexture.width, debugTexture.height, TextureFormat.RGBAFloat, false);
        readTexture.ReadPixels(new Rect(0, 0, debugTexture.width, debugTexture.height), 0, 0);
        readTexture.Apply();
        RenderTexture.active = null;

    
        

        // Read back result texture and find the best angles
        RenderTexture.active = renderTexture;
        readTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false);
        readTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readTexture.Apply();
        RenderTexture.active = null;



        float minError = float.MaxValue;
        float bestJ1 = 0, bestJ2 = 0;

        float maxJ1 = 0;
        float maxJ2 = 0;
        for (int i = 0; i < readTexture.width; i++) {
            for (int j = 0; j < readTexture.height; j++) {
                Color pixel = readTexture.GetPixel(i, j);
                if (pixel.r > maxJ1) {
                    maxJ1 = pixel.r;
                }
                if (pixel.g > maxJ2) {
                    maxJ2 = pixel.g;
                }
                if (pixel.r < minError) {
                    minError = pixel.r;
                    bestJ1 = pixel.g; // Angles in radians
                    bestJ2 = pixel.b;
                }
            }
        }
        // Debug.Log($"Max J1: {maxJ1*360.0}, Max J2: {maxJ2*360.0}");

        bestJ1 = bestJ1*360f;
        bestJ2 = bestJ2*360f;

        // Apply rotations to joints
        joint1.jointObject.transform.localRotation = Quaternion.Euler(0, 0, bestJ1); // Assuming rotation around Y-axis
        joint2.jointObject.transform.localRotation = Quaternion.Euler(0, 0, bestJ2); // Adjust axis as necessary

        // Debug.Log($"Applied rotations - Joint1: {bestJ1} degrees, Joint2: {bestJ2} degrees");
        if(Input.GetKeyDown(KeyCode.Space)){
            SaveRenderTextureToPNG();
        }
        // Clean up
        Destroy(readTexture);
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

