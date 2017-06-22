using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kino;
 
public class CameraController : UnitySingleton<CameraController>
{
	public Transform playerTrans;
	public AnalogGlitch analogGlitch;
	public LayerMask collisionLayers = new LayerMask();
	public Vector3 targetOffset = new Vector3 ();
    public Vector3 posOffset = new Vector3();
	public Vector3 rotateOffset = new Vector3();

	private Vector2 _originRotation = new Vector2();
	public bool returnToOrigin = true;
	public float returnSmoothing = 3;

	public float distance = 5;
	public float minDistance = 0;
	public float maxDistance = 10;

	public Vector2 sensitivity = new Vector2(3, 3);

	public float zoomSpeed = 1;
	public float zoomSmoothing = 16;

	public float minAngle = -90;
	public float maxAngle = 90;


	private float _wanted_distance;
	private Quaternion _rotation;
	private Vector2 _input_rotation;

	private Pawn _player;
	private bool _isCharacter;
	private float cDis = 5;
	private float vDis = 20;
	private Vector2 cOffset = new Vector2 (0f,1.5f);
	private Vector2 vOffset = new Vector2 (0f,4f);
	private Camera _camera;
	void Start()
	{
		_camera = GetComponent<Camera> ();
		analogGlitch = GetComponent<AnalogGlitch> ();
		_wanted_distance = distance;
		_input_rotation = _originRotation;
	}

	public void ChangeToCharacter()
	{
		vDis = _wanted_distance;
		_wanted_distance = cDis;
		_isCharacter = true;
		targetOffset = cOffset;
		minDistance = -1.2f;
		maxDistance = 20f;
		_camera.nearClipPlane = 0.01f;
		_camera.farClipPlane= 4000f;
	}
	public void ChangeToVehicle()
	{
		cDis = _wanted_distance;
		_wanted_distance = vDis;
		_isCharacter = false;
		targetOffset = vOffset;
		minDistance = 0f;
		maxDistance = 60f;
		if(_wanted_distance<=0.1f)
		{
			_camera.nearClipPlane = 15f;
		}
		else
		{
			_camera.nearClipPlane = 5f;
		}
		_camera.farClipPlane= 40000f;
	}
	public void LerpRotateOffset(Vector3 v)
	{
		rotateOffset = Vector3.Lerp (rotateOffset,v,4f*Time.deltaTime);
	}
	public void LerpPosOffset(Vector3 v)
	{
		posOffset = Vector3.Lerp (posOffset,v,4f*Time.deltaTime);
	}
	void LateUpdate()
	{
		playerTrans = GameManager.Instance.playerTrans;
		if (playerTrans == null)
			return;
		_player = GameManager.Instance.player;
		_isCharacter = _player is Character;
		// Zoom control
		if(InputManager.Instance.GetZoom()==1)
		{
			_wanted_distance += zoomSpeed*Time.deltaTime;
			if (!_isCharacter) {
				_wanted_distance = Mathf.Max (_wanted_distance,15f);
				_camera.nearClipPlane = 5f;
			}
		}
		else if(InputManager.Instance.GetZoom()==-1)
		{
			_wanted_distance -= zoomSpeed*Time.deltaTime;
			if (!_isCharacter &&_wanted_distance <= 15f)
				_wanted_distance = 0f;
			if (!_isCharacter && _wanted_distance <= 0.1f) {
				_camera.nearClipPlane = 15f;
			}
		}

		// Prevent wanted distance from going below or above min and max distance
		_wanted_distance = Mathf.Clamp(_wanted_distance, minDistance, maxDistance);

		// If user clicks, change position based on drag direction and sensitivity
		// Stop at 90 degrees above / below object
		if (InputManager.Instance.IsScrolling()) {
			_input_rotation.x += InputManager.Instance.GetScrollWheel ().x * sensitivity.x;
			ClampRotation ();
			_input_rotation.y -= InputManager.Instance.GetScrollWheel ().y * sensitivity.y;
			_input_rotation.y = Mathf.Clamp (_input_rotation.y, minAngle, maxAngle);
		} else if(!_isCharacter){
			_originRotation = _input_rotation;
			_originRotation.x = 0f;
			ClampRotation ();
			_input_rotation = Vector3.Lerp (_input_rotation, _originRotation, returnSmoothing * Time.deltaTime);
		}
		_rotation = Quaternion.Euler(new Vector3(_input_rotation.y, _input_rotation.x, 0f)+rotateOffset);


		distance = Mathf.Clamp(Mathf.Lerp(distance, _wanted_distance, Time.deltaTime * zoomSmoothing), minDistance, maxDistance);

		Vector3 wanted_position = _rotation * (new Vector3 (targetOffset.x+posOffset.x, 0f, -_wanted_distance - 0.2f))+new Vector3(0f,targetOffset.y+posOffset.y,0f);
		wanted_position = playerTrans.TransformPoint (wanted_position);
		Vector3 current_position = _rotation * new Vector3(targetOffset.x+posOffset.x,0f,0f)+new Vector3(0f,targetOffset.y+posOffset.y,0f);
		current_position = playerTrans.TransformPoint (current_position);

		RaycastHit hit;
		if(Physics.Linecast(current_position, wanted_position, out hit, collisionLayers))
		{
			distance = Vector3.Distance(current_position, hit.point) - 0.2f;
		}
			
		// Set the position and rotation of the camera
		wanted_position = _rotation * (new Vector3 (targetOffset.x, 0f, -distance))+new Vector3(0f,targetOffset.y,0f)+posOffset;
		transform.position = playerTrans.TransformPoint (wanted_position);
		transform.rotation = playerTrans.rotation * _rotation;
		//transform.position = Vector3.Lerp(transform.position,playerTrans.TransformPoint (wanted_position),10f*Time.deltaTime);
		//transform.rotation = Quaternion.Lerp(transform.rotation,playerTrans.rotation * _rotation,10f*Time.deltaTime);
		if(_isCharacter)
			((Character)_player).rotation = _input_rotation.x;
		GameManager.Instance.CrossUpdate ();
	}

    private void ClampRotation()
    {
        if(_originRotation.x < -180)
        {
            _originRotation.x += 360;
        }
        else if(_originRotation.x > 180)
        {
            _originRotation.x -= 360;
        }

        if(_input_rotation.x - _originRotation.x < -180)
        {
            _input_rotation.x += 360;
        }
        else if(_input_rotation.x - _originRotation.x > 180)
        {
            _input_rotation.x -= 360;
        }
    }
}
