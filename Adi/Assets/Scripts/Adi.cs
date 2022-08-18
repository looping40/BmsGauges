using UnityEngine;
using F4SharedMem;
using F4SharedMem.Headers;
using SpaNs;
using System;
using UnityEngine.UIElements.Experimental;

public class Adi : MonoBehaviour
{

    //debug
    [SerializeField] bool useDebug = false;

    //Ball
    [SerializeField] float pitchAngle;
    [SerializeField] float rollAngle;

    [SerializeField] float heading;

    //Slip
    [SerializeField] float slip;

    //turn rate
    [SerializeField] float turnRate;

    //Ils
    [SerializeField] float glideAngle;
    [SerializeField] float locAngle;
    
    //Flags
    [SerializeField] bool showGsFlag;
    [SerializeField] bool showLocFlag;
    [SerializeField] bool showOffFlag;
    [SerializeField] bool showAuxFlag;

    [SerializeField] uint instrLight;

    //Own aricraft
    private GameObject _aircarftGameObject;
    private GameObject _sunGameObject;
    private GameObject _sunLightGameObject;

    //Ball
    private GameObject _BallGameObject;

    //Bank
    private GameObject _bankGameObject;

    //Slip
    private GameObject _SlipGameObject;

    //turn rate
    private GameObject _TurnRateGameObject;

    //Ils
    private GameObject _GsGameObject;
    private GameObject _LocGameObject;

    //Flags
    private GameObject _FlagGsGameObject;
    private GameObject _FlagLocGameObject;
    private GameObject _FlagOffGameObject;
    private GameObject _FlagAuxGameObject;

    Light mySunLight;
    Material _BallMaterial;
    Material _LocMaterial;
    Material _GsMaterial;

    Material _FlagLocMaterial;
    Material _FlagGsMaterial;
    Material _FlagAuxMaterial;
    Material _FlagOffMaterial;

    private readonly Reader _sharedMemReader = new();
    private FlightData _lastFlightData;

    private const float GLIDESLOPE_SCALE = 5.0f;
    private const float LOCALIZER_SCALE = 1.0f;
    private const float RADIANS_PER_DEGREE = 0.0174532925f;

    private Camera _cam;
    private float _camera_x_scale = 1.0f;
    private float _mouse_prev_scroll = 0;
    private Matrix4x4 _DefaultViewMatrix = new(
                                        new Vector4(4.166667f, 0.0f, 0.0f, 0.0f),
                                        new Vector4(0.0f, 4.166667f, 0.0f, 0.0f),
                                        new Vector4(0.0f, 0.0f, -1.0006f, -1.0f),
                                        new Vector4(0.0f, 0.0f, -0.60018f, 0.0f)
                                    );
    private Quaternion _DefaultSunRotation = Quaternion.Euler(new Vector3(-122.764f, 23.26199f, 0));
    private Quaternion _DefaultSunLightRotation = Quaternion.Euler(new Vector3(4.206f, 182.155f, - 176.991f));

    private readonly Spa SpaObject = new();

    private FlightData ReadSharedMem()
    {
        return _lastFlightData = _sharedMemReader.GetCurrentData();
    }

    private float RadToDeg(float angle)
    {
        return angle / RADIANS_PER_DEGREE;
    }

    // Start is called before the first frame update
    void Start()
    {
        _BallGameObject = GameObject.Find("Ball");
        _bankGameObject = GameObject.Find("Bank");
        _SlipGameObject = GameObject.Find("Slip");
        _TurnRateGameObject  = GameObject.Find("TurnRate");
        _GsGameObject = GameObject.Find("Gs");
        _LocGameObject = GameObject.Find("Loc");
        _FlagGsGameObject = GameObject.Find("FlagGs");
        _FlagLocGameObject = GameObject.Find("FlagLoc");
        _FlagOffGameObject = GameObject.Find("FlagOff");
        _FlagAuxGameObject = GameObject.Find("FlagAux");
        _aircarftGameObject = GameObject.Find("Aircraft");
        _sunGameObject = GameObject.Find("Sun");
        _sunLightGameObject = GameObject.Find("LightSun");
        mySunLight = _sunLightGameObject.GetComponent<Light>();

        _BallMaterial = _BallGameObject.GetComponent<Renderer>().material;

        _LocMaterial = _LocGameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;
        _GsMaterial = _GsGameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;

        _FlagLocMaterial = _FlagGsGameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().material; 
        _FlagGsMaterial = _FlagLocGameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;
        _FlagAuxMaterial = _FlagOffGameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;
        _FlagOffMaterial = _FlagAuxGameObject.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;

        _cam = Camera.main;
        LoadProjectionMatrix();
        
        SpaObject.spa.year = 2022;
        SpaObject.spa.month = 8;
        SpaObject.spa.day = 1;
        SpaObject.spa.timezone = +9;
        SpaObject.spa.function = SpaFunction.ZA;

        
    }


    void Update()
    {
        UpdateAspectRation();

        Update_spa();

        //Update Adi
        UpdateAdi();

        //Update day/light
        UpdateLight();
        
    }

    
    private void OnApplicationQuit()
    {
        SaveProjectionMatrix();
    }

    //change window aspect ration
    private void UpdateAspectRation()
    {
        var curScroll = Input.GetAxis("Mouse ScrollWheel");
        if ((_mouse_prev_scroll - curScroll != 0) & Input.GetKey(KeyCode.LeftShift))
        {
            _camera_x_scale += (_mouse_prev_scroll - curScroll) / 10;
            _mouse_prev_scroll = curScroll;
        }
        _cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(_camera_x_scale, 1, 1));
    }

    private void LoadProjectionMatrix()
    {
        for (int i = 0; i< 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                _DefaultViewMatrix[i,j] = PlayerPrefs.GetFloat("M"+i+"_"+j, _DefaultViewMatrix[i,j]);
                
            }
        }

        _cam.projectionMatrix = _DefaultViewMatrix;
    }

    private void SaveProjectionMatrix()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                PlayerPrefs.SetFloat("M" + i + "_" + j, _cam.projectionMatrix[i, j]);

            }
        }
    }


    private void UpdateAdi()
    {
        if (useDebug)
        {
            _aircarftGameObject.transform.localRotation = Quaternion.Euler(new Vector3(pitchAngle, 180+heading, rollAngle));

            _BallGameObject.transform.localRotation = Quaternion.Euler(new Vector3(pitchAngle, 0, rollAngle));
            _bankGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, rollAngle));

            _SlipGameObject.transform.localPosition = new Vector3(slip, _SlipGameObject.transform.localPosition.y, _SlipGameObject.transform.localPosition.z);
            _TurnRateGameObject.transform.localPosition = new Vector3(turnRate * 2, _TurnRateGameObject.transform.localPosition.y, _TurnRateGameObject.transform.localPosition.z);


            _GsGameObject.transform.localRotation = Quaternion.Euler(new Vector3(glideAngle, 0, 0));
            _LocGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, locAngle, 0));

            if (showGsFlag)
            {
                _FlagGsGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -30));
            }
            else
            {
                _FlagGsGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }

            if (showLocFlag)
            {
                _FlagLocGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 30));
            }
            else
            {
                _FlagLocGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }

            if (showOffFlag)
            {
                _FlagOffGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 30));
            }
            else
            {
                _FlagOffGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }

            if (showOffFlag || showAuxFlag)
            {
                _FlagAuxGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -30));
            }
            else
            {
                _FlagAuxGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }
        }
        else
        {
            if (ReadSharedMem() != null)
            {
                _aircarftGameObject.transform.localRotation = Quaternion.Euler(new Vector3(RadToDeg(_lastFlightData.pitch), 180 + _lastFlightData.currentHeading, -RadToDeg(_lastFlightData.roll)));


                _BallGameObject.transform.localRotation = Quaternion.Euler(new Vector3(RadToDeg(_lastFlightData.pitch), 0, -RadToDeg(_lastFlightData.roll)));
                _bankGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -RadToDeg(_lastFlightData.roll)));

                _SlipGameObject.transform.localPosition = new Vector3(_lastFlightData.beta, _SlipGameObject.transform.localPosition.y, _SlipGameObject.transform.localPosition.z);

                _GsGameObject.transform.localRotation = Quaternion.Euler(new Vector3(GLIDESLOPE_SCALE * RadToDeg(-_lastFlightData.AdiIlsVerPos), 0, 0));
                _LocGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, LOCALIZER_SCALE * RadToDeg(-_lastFlightData.AdiIlsHorPos), 0));

                _TurnRateGameObject.transform.localPosition = new Vector3(_lastFlightData.turnRate * 0.4f, _TurnRateGameObject.transform.localPosition.y, _TurnRateGameObject.transform.localPosition.z);

                var hsiBits = (HsiBits)_lastFlightData.hsiBits;
                bool _showAuxFlag = ((hsiBits & HsiBits.ADI_AUX) == HsiBits.ADI_AUX) || ((hsiBits & HsiBits.ADI_OFF) == HsiBits.ADI_OFF);
                bool _showOffFlag = (hsiBits & HsiBits.ADI_OFF) == HsiBits.ADI_OFF;
                bool _showGsFlag = (hsiBits & HsiBits.ADI_GS) == HsiBits.ADI_GS;
                bool _showLocFlag = (hsiBits & HsiBits.ADI_LOC) == HsiBits.ADI_LOC;

                if (_showGsFlag)
                {
                    _FlagGsGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -30));
                }
                else
                {
                    _FlagGsGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                }

                if (_showLocFlag)
                {
                    _FlagLocGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 30));
                }
                else
                {
                    _FlagLocGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                }

                if (_showOffFlag)
                {
                    _FlagOffGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 30));
                }
                else
                {
                    _FlagOffGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                }

                if (_showAuxFlag)
                {
                    _FlagAuxGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -30));
                }
                else
                {
                    _FlagAuxGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                }
            }
        }

    }

    //Sun position
    private void Update_spa()
    {
        if (useDebug)
        {
            _sunGameObject.transform.rotation = _DefaultSunRotation;
            _sunLightGameObject.transform.rotation = _DefaultSunLightRotation;
        }
        else
        {
            if (ReadSharedMem() != null)
            {
                TimeSpan t = TimeSpan.FromSeconds(_lastFlightData.currentTime);
                SpaObject.spa.hour = t.Hours;
                SpaObject.spa.minute = t.Minutes;
                SpaObject.spa.second = t.Seconds;

                SpaObject.spa.longitude = _lastFlightData.longitude;
                SpaObject.spa.latitude = _lastFlightData.latitude;

                SpaObject.spa.year = 2022;
                SpaObject.spa.month = 8;
                SpaObject.spa.day = 1;
                SpaObject.spa.timezone = +9;
                SpaObject.spa.function = SpaFunction.ZA;

                SpaObject.Spa_calculate();

                _sunGameObject.transform.rotation = Quaternion.Euler(new Vector3(-(float)SpaObject.spa.zenith, (float)SpaObject.spa.azimuth, 0));
            }
        }

        _sunLightGameObject.transform.LookAt(_BallGameObject.transform);
    }

    private void UpdateLight()
    {
        if (useDebug)
        {
            Light myLight = _sunLightGameObject.GetComponent<Light>();
            if (instrLight != 0)
            {
                myLight.intensity = 0;
                _BallMaterial.EnableKeyword("_EMISSION");

                _LocMaterial.EnableKeyword("_EMISSION");
                _GsMaterial.EnableKeyword("_EMISSION");

                _FlagLocMaterial.EnableKeyword("_EMISSION");
                _FlagGsMaterial.EnableKeyword("_EMISSION");
                _FlagAuxMaterial.EnableKeyword("_EMISSION");
                _FlagOffMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                myLight.intensity = 1;
                _BallMaterial.DisableKeyword("_EMISSION");

                _LocMaterial.DisableKeyword("_EMISSION");
                _GsMaterial.DisableKeyword("_EMISSION");

                _FlagLocMaterial.DisableKeyword("_EMISSION");
                _FlagGsMaterial.DisableKeyword("_EMISSION");
                _FlagAuxMaterial.DisableKeyword("_EMISSION");
                _FlagOffMaterial.DisableKeyword("_EMISSION");
            }
        }
        else
        {
            if (ReadSharedMem() != null)
            {
                Light mySunLight = _sunLightGameObject.GetComponent<Light>();
                if (_lastFlightData.instrLight != 0)
                {
                    mySunLight.intensity = 0;
                    _BallMaterial.EnableKeyword("_EMISSION");

                    _LocMaterial.EnableKeyword("_EMISSION");
                    _GsMaterial.EnableKeyword("_EMISSION");

                    _FlagLocMaterial.EnableKeyword("_EMISSION");
                    _FlagGsMaterial.EnableKeyword("_EMISSION");
                    _FlagAuxMaterial.EnableKeyword("_EMISSION");
                    _FlagOffMaterial.EnableKeyword("_EMISSION");
                }
                else
                {
                    mySunLight.intensity = 1;
                    _BallMaterial.DisableKeyword("_EMISSION");

                    _LocMaterial.DisableKeyword("_EMISSION");
                    _GsMaterial.DisableKeyword("_EMISSION");

                    _FlagLocMaterial.DisableKeyword("_EMISSION");
                    _FlagGsMaterial.DisableKeyword("_EMISSION");
                    _FlagAuxMaterial.DisableKeyword("_EMISSION");
                    _FlagOffMaterial.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
