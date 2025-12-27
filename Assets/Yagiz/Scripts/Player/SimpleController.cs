using UnityEngine;

public class SimpleController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;

    private Vector3 movedir;
    private float rotationY;
    void Start()
    {

    }

    void Update()
    {
        HandleMovemennt();
        HandleRotation();
    }

    private void HandleMovemennt()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        movedir = new Vector3(horizontal, 0f, vertical).normalized;
        transform.Translate(movedir * speed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        rotationY += mouseX * mouseSensitivity;

        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }
}
