using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;  // Para el nuevo sistema de entradas
using UnityEngine.EventSystems;

public class ARInteractionManager : MonoBehaviour
{
    [SerializeField] private Camera aRCamera;
    private ARRaycastManager aRRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject aRPointer;
    private GameObject item3DModel;
    private GameObject itemSelected;

    private bool isInitialPosition;
    private bool isOverUI;
    private bool isOver3DModel;

    private Vector2 initialTouchPos;

    // Referencias para el Input Action
    public InputActionAsset inputActionAsset;  // El asset de acciones
    private InputActionMap inputActionMap;     // El mapa de acciones
    private InputAction touchAction;           // Acción para el toque

    public GameObject Item3DModel
    {
        set
        {
            item3DModel = value;
            item3DModel.transform.position = aRPointer.transform.position;
            item3DModel.transform.parent = aRPointer.transform;
            isInitialPosition = true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        aRPointer = transform.GetChild(0).gameObject;
        aRRaycastManager = FindObjectOfType<ARRaycastManager>();
        GameManager.instance.OnMainMenu += SetItemPosition;

        // Inicializar y habilitar las acciones de entrada
        inputActionMap = inputActionAsset.FindActionMap("TouchActions");  // Nombre del mapa de acciones
        touchAction = inputActionMap.FindAction("Touch");  // Nombre de la acción de toque
        touchAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitialPosition)
        {
            Vector2 middlePointScreen = new Vector2(Screen.width / 2, Screen.height / 2);
            aRRaycastManager.Raycast(middlePointScreen, hits, TrackableType.Planes);
            if (hits.Count > 0)
            {
                transform.position = hits[0].pose.position;
                transform.rotation = hits[0].pose.rotation;
                aRPointer.SetActive(false);
            }
        }

        if (touchAction.ReadValue<Vector2>().magnitude > 0)  // Detecta si hay un toque
        {
            Vector2 touchPosition = touchAction.ReadValue<Vector2>();

            if (touchPosition != Vector2.zero)
            {
                isOverUI = IsTapOverUI(touchPosition);
                isOver3DModel = IsTapOver3DModel(touchPosition);
            }
        }

        if (touchAction.IsPressed())
        {
            if (aRRaycastManager.Raycast(touchAction.ReadValue<Vector2>(), hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;
                if (!isOverUI && isOver3DModel)
                {
                    transform.position = hitPose.position;
                }
            }
        }

        // Lógica de rotación con dos toques
        if (Input.touchCount == 2)
        {
            Touch touchOne = Input.GetTouch(0);
            Touch touchTwo = Input.GetTouch(1);

            if (touchOne.phase == UnityEngine.TouchPhase.Began || touchTwo.phase == UnityEngine.TouchPhase.Began)  // Cambié aquí
            {
                initialTouchPos = touchTwo.position - touchOne.position;
            }

            if (touchOne.phase == UnityEngine.TouchPhase.Moved || touchTwo.phase == UnityEngine.TouchPhase.Moved)  // Cambié aquí
            {
                Vector2 currentTouchPos = touchTwo.position - touchOne.position;
                float angle = Vector2.SignedAngle(initialTouchPos, currentTouchPos);
                item3DModel.transform.rotation = Quaternion.Euler(0, item3DModel.transform.eulerAngles.y - angle, 0);
                initialTouchPos = currentTouchPos;
            }
        }

        if (isOver3DModel && item3DModel == null && !isOverUI)
        {
            GameManager.instance.ARPosition();
            item3DModel = itemSelected;
            itemSelected = null;
            aRPointer.SetActive(true);
            transform.position = item3DModel.transform.position;
            item3DModel.transform.parent = aRPointer.transform;
        }
    }

    private bool IsTapOver3DModel(Vector2 touchPosition)
    {
        Ray ray = aRCamera.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit3DModel))
        {
            if (hit3DModel.collider.CompareTag("Item"))
            {
                itemSelected = hit3DModel.transform.gameObject;
                return true;
            }
        }

        return false;
    }

    private bool IsTapOverUI(Vector2 touchPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = new Vector2(touchPosition.x, touchPosition.y);

        List<RaycastResult> result = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, result);

        return result.Count > 0;
    }

    private void SetItemPosition()
    {
        if (item3DModel != null)
        {
            item3DModel.transform.parent = null;
            aRPointer.SetActive(false);
            item3DModel = null;
        }
    }

    public void DeleteItem()
    {
        Destroy(item3DModel);
        aRPointer.SetActive(false);
        GameManager.instance.MainMenu();
    }

    // Deshabilita las acciones al salir
    void OnDisable()
    {
        touchAction.Disable();
    }
}
