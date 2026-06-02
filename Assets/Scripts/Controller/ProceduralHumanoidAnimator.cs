using UnityEngine;

public class ProceduralHumanoidAnimator : MonoBehaviour
{
    [Header("Exposed Bones")]
    public Transform hips;
    public Transform spine;
    public Transform leftUpperArm;
    public Transform rightUpperArm;
    public Transform leftForearm;
    public Transform rightForearm;
    public Transform leftThigh;
    public Transform rightThigh;
    public Transform leftCalf;
    public Transform rightCalf;

    [Header("Idle Animation Settings")]
    public float breathingSpeed = 2.0f;
    public float breathingAngle = 2.0f;
    public float idleArmAngle = 75.0f; // Angle to lower the arms from T-pose (degrees)

    [Header("Gait / Running Settings")]
    public float gaitSpeedMultiplier = 3.0f;
    public float maxLegSwing = 25.0f;     // Max thigh angle forward/backward (degrees)
    public float maxKneeBend = 35.0f;     // Max knee bend angle (degrees)
    public float maxArmSwing = 35.0f;     // Max arm swing forward/backward (degrees)
    public float bobHeight = 0.08f;       // Up/down hip bobbing height (meters)
    public float armSwingOffset = 10f;    // Keep arms slightly away from the body during runs

    private CharacterController charController;
    private float gaitCycle = 0f;

    // Default T-pose transforms
    private Vector3 hipsDefaultPos;
    private Quaternion hipsDefaultRot;
    private Quaternion spineDefaultRot;
    private Quaternion leftUpperArmDefaultRot;
    private Quaternion rightUpperArmDefaultRot;
    private Quaternion leftForearmDefaultRot;
    private Quaternion rightForearmDefaultRot;
    private Quaternion leftThighDefaultRot;
    private Quaternion rightThighDefaultRot;
    private Quaternion leftCalfDefaultRot;
    private Quaternion rightCalfDefaultRot;

    // Smooth blending velocities
    private float currentBlendSpeed = 0f;

    private void Start()
    {
        charController = GetComponent<CharacterController>();

        // Auto-detect bones if they are not assigned in the inspector
        FindBones();

        // Save original T-pose rotations & positions
        SaveDefaultPose();
    }

    private void LateUpdate()
    {
        if (hips == null || leftUpperArm == null || rightUpperArm == null || 
            leftThigh == null || rightThigh == null || leftCalf == null || rightCalf == null)
        {
            return;
        }

        // Get horizontal speed from CharacterController
        float speed = 0f;
        if (charController != null)
        {
            Vector3 horizontalVelocity = new Vector3(charController.velocity.x, 0f, charController.velocity.z);
            speed = horizontalVelocity.magnitude;
        }

        // Smoothly blend movement speed value for animations
        currentBlendSpeed = Mathf.Lerp(currentBlendSpeed, speed, Time.deltaTime * 8f);

        // Breathing cycle (sine wave)
        float breathing = Mathf.Sin(Time.time * breathingSpeed) * breathingAngle;

        // Run gait cycle time accumulator
        gaitCycle += Time.deltaTime * currentBlendSpeed * gaitSpeedMultiplier;

        if (currentBlendSpeed > 0.1f)
        {
            // 1. RUNNING CYCLE
            float movementFactor = Mathf.Clamp01(currentBlendSpeed / 6f); // Normalize against running speed

            // Thighs (hips) swing forward/backward (opposing phases)
            float thighSwingAngle = Mathf.Sin(gaitCycle) * maxLegSwing * movementFactor;
            leftThigh.localRotation = leftThighDefaultRot * Quaternion.Euler(thighSwingAngle, 0f, 0f);
            rightThigh.localRotation = rightThighDefaultRot * Quaternion.Euler(-thighSwingAngle, 0f, 0f);

            // Knees (calves) bend back when the leg swings backward to lift feet
            float leftKneeAngle = (Mathf.Cos(gaitCycle) + 1f) * 0.5f * maxKneeBend * movementFactor;
            float rightKneeAngle = (-Mathf.Cos(gaitCycle) + 1f) * 0.5f * maxKneeBend * movementFactor;
            leftCalf.localRotation = leftCalfDefaultRot * Quaternion.Euler(-leftKneeAngle, 0f, 0f);
            rightCalf.localRotation = rightCalfDefaultRot * Quaternion.Euler(-rightKneeAngle, 0f, 0f);

            // Arms swing forward/backward (opposing thigh phases)
            float armSwingAngle = Mathf.Sin(gaitCycle) * maxArmSwing * movementFactor;
            // Lower arms down (idleArmAngle) and swing them
            leftUpperArm.localRotation = leftUpperArmDefaultRot * Quaternion.Euler(armSwingAngle, 0f, -idleArmAngle + armSwingOffset + Mathf.Abs(armSwingAngle) * 0.1f);
            rightUpperArm.localRotation = rightUpperArmDefaultRot * Quaternion.Euler(-armSwingAngle, 0f, idleArmAngle - armSwingOffset - Mathf.Abs(armSwingAngle) * 0.1f);

            // Elbows bent during runs
            if (leftForearm != null) leftForearm.localRotation = leftForearmDefaultRot * Quaternion.Euler(30f, 0f, 0f);
            if (rightForearm != null) rightForearm.localRotation = rightForearmDefaultRot * Quaternion.Euler(30f, 0f, 0f);

            // Spine leans forward slightly when running
            spine.localRotation = spineDefaultRot * Quaternion.Euler(5f * movementFactor + breathing * 0.2f, 0f, 0f);

            // Bob the hips vertically up/down
            float bob = Mathf.Abs(Mathf.Sin(gaitCycle)) * bobHeight * movementFactor;
            hips.localPosition = hipsDefaultPos - Vector3.up * bob;
        }
        else
        {
            // 2. IDLE CYCLE (Standing with arms down & breathing)
            
            // Hang upper arms naturally down from default horizontal T-pose
            leftUpperArm.localRotation = leftUpperArmDefaultRot * Quaternion.Euler(0f, 0f, -idleArmAngle + breathing * 0.2f);
            rightUpperArm.localRotation = rightUpperArmDefaultRot * Quaternion.Euler(0f, 0f, idleArmAngle - breathing * 0.2f);

            // Elbows relaxed / slightly bent
            if (leftForearm != null) leftForearm.localRotation = leftForearmDefaultRot * Quaternion.Euler(15f, 0f, 0f);
            if (rightForearm != null) rightForearm.localRotation = rightForearmDefaultRot * Quaternion.Euler(15f, 0f, 0f);

            // Breathe gently through the spine
            spine.localRotation = spineDefaultRot * Quaternion.Euler(breathing, 0f, 0f);

            // Reset hips, thighs, and calves to defaults
            hips.localPosition = hipsDefaultPos;
            hips.localRotation = hipsDefaultRot;
            leftThigh.localRotation = leftThighDefaultRot;
            rightThigh.localRotation = rightThighDefaultRot;
            leftCalf.localRotation = leftCalfDefaultRot;
            rightCalf.localRotation = rightCalfDefaultRot;
        }
    }

    private void FindBones()
    {
        if (hips == null) hips = FindChildRecursive(transform, "CC_Base_Waist");
        if (hips == null) hips = FindChildRecursive(transform, "CC_Base_Pelvis");
        if (spine == null) spine = FindChildRecursive(transform, "CC_Base_Spine01");
        if (leftUpperArm == null) leftUpperArm = FindChildRecursive(transform, "CC_Base_L_Upperarm");
        if (rightUpperArm == null) rightUpperArm = FindChildRecursive(transform, "CC_Base_R_Upperarm");
        if (leftForearm == null) leftForearm = FindChildRecursive(transform, "CC_Base_L_Forearm");
        if (rightForearm == null) rightForearm = FindChildRecursive(transform, "CC_Base_R_Forearm");
        if (leftThigh == null) leftThigh = FindChildRecursive(transform, "CC_Base_L_Thigh");
        if (rightThigh == null) rightThigh = FindChildRecursive(transform, "CC_Base_R_Thigh");
        if (leftCalf == null) leftCalf = FindChildRecursive(transform, "CC_Base_L_Calf");
        if (rightCalf == null) rightCalf = FindChildRecursive(transform, "CC_Base_R_Calf");
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void SaveDefaultPose()
    {
        if (hips != null)
        {
            hipsDefaultPos = hips.localPosition;
            hipsDefaultRot = hips.localRotation;
        }
        if (spine != null) spineDefaultRot = spine.localRotation;
        if (leftUpperArm != null) leftUpperArmDefaultRot = leftUpperArm.localRotation;
        if (rightUpperArm != null) rightUpperArmDefaultRot = rightUpperArm.localRotation;
        if (leftForearm != null) leftForearmDefaultRot = leftForearm.localRotation;
        if (rightForearm != null) rightForearmDefaultRot = rightForearm.localRotation;
        if (leftThigh != null) leftThighDefaultRot = leftThigh.localRotation;
        if (rightThigh != null) rightThighDefaultRot = rightThigh.localRotation;
        if (leftCalf != null) leftCalfDefaultRot = leftCalf.localRotation;
        if (rightCalf != null) rightCalfDefaultRot = rightCalf.localRotation;
    }
}
