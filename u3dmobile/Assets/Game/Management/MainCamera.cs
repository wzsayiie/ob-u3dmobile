using UnityEngine;

namespace U3DMobile
{
    public class MainCamera : SingletonBehaviour<MainCamera>
    {
        public static MainCamera instance { get { return GetInstance(); } }

        private Camera _mainCamera;

        private Camera FindMainCamera()
        {
            if (_mainCamera != null)
            {
                return _mainCamera;
            }

            //first try find the camera named "MainCamera" in scene.
            GameObject cameraObject = GameObject.Find("MainCamera");
            if (cameraObject != null)
            {
                cameraObject.TryGetComponent(out _mainCamera);
            }

            //when there is no suitbale camera, create new one.
            if (_mainCamera == null)
            {
                _mainCamera = gameObject.AddComponent<Camera>();
            }

            return _mainCamera;
        }

        public Camera mainCamera
        {
            get
            {
                return FindMainCamera();
            }
        }

        public bool isReferenceExternal
        {
            get
            {
                return mainCamera.gameObject != gameObject;
            }
        }
    }
}
