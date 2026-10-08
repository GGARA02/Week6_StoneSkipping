using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using Unity.Cinemachine;
using UnityEngine;

public enum ArrowState
{
    None,
    BulletTime,
    Dash,
    HyperDash
}

public class ArrowController : MonoBehaviour
{
    //TODO : 쉐이더 그래프로 깜빡임 효과
    //TODO : currentSpeed는 최대 속도 제한으로 한다. 가속은 벡터 투영으로, 감속은 벡터의 반대방향으로?
    //[SerializeField]
    //private SmallArrowSpiralMove wispTail;
    [Header("Default")]
    [SerializeField]
    private float speed;
    [SerializeField]
    private float invincibleTime; //벽 피격 후 무적 시간
    [SerializeField]
    private float accel; //가속 변수
    [SerializeField]
    private float decel; //감속 변수
    [SerializeField]
    private float verticalRatio = 0.2f;
    [Header("Dash")]
    [SerializeField]
    private float dashSpeed;
    [SerializeField]
    private float dashCoolTime; //대시 유지 시간
    [Header("HyperDash")]
    [SerializeField]
    private float hyperDashSpeed;
    [SerializeField]
    private float hyperDashCoolTime; //하이퍼대시 유지 시간
    [Header("BulletTime")]
    [SerializeField]
    private float startBulletTime; //시작 게이지 (체력 겸 불릿타임 자원)
    [SerializeField]
    private float maxBulletTime;
    [SerializeField]
    private float bulletTimeSpeed;
    [SerializeField]
    private float bulletTimeScale; //불릿타임 중 Time.timeScale
    [SerializeField]
    private float bulletTimeDiscount; //초당 기본 게이지 소모량
    [SerializeField]
    private float bulletTimeDuringDiscount; //불릿타임 중 추가 소모 배율
    [SerializeField]
    private float bulletTimeLightUp; //불씨 획득 시 회복량
    [SerializeField]
    private float gainPerSecond = 1f; //불씨 획득 시 최대 초당 회복량
    public float BulletTimeLightUp => bulletTimeLightUp;
    [SerializeField]
    private float bulletWallHit; //벽 피격 시 감소량
    [Header("Difficult")]
    [SerializeField]
    private float difficultSpeedUp; //초당 속도 증가량
    [Header("Trail")]
    [SerializeField]
    private TrailRenderer trail;
    [SerializeField]
    private ParticleSystem particle;
    [Header("Boost")]
    [SerializeField]
    private int startBoostCount = 1;
    [SerializeField]
    private float boostCoolTime = 3.0f;
    [SerializeField]
    private GameObject emberGainEffect;
    [Header("Env")]
    [SerializeField]
    private float envAccel;
    [Header("Idle")]
    [SerializeField]
    private float idleCircleRadius = 3f;
    [SerializeField]
    private float idleCircleSpeed = 1.5f;
    [SerializeField]
    float idleCircleEnterLerp = 3f;

    private bool isIdleCircling = false;
    private Vector3 idleCenterPosition;
    private float idleCurrentAngle = 0f;



    private ArrowState arrowState;
    private float currentSpeed;
    private float remainCoolTime; //남은 쿨타임은 조작불가능 시간과 동일하다.
    private float remainInvincibleTime;
    private float remainBulletTime; //0이 되면 게임오버
    private bool isActive = false; //false면 조작과 게이지 소모가 멈춘다.

    //private InputManager input;
    private Transform brainTransform; //이동 기준이 되는 메인 카메라(시네머신 브레인)
    private CharacterController characterController;

    public System.Action OnHitWall; //플레이어 히트처리
    public System.Action<ArrowState> OnArrowStateChange; //카메라에서 상태별 연출을 위한 이벤트
    public System.Action OnLightUp; //불씨를 밝히자
    public System.Action OnGameOver;

    public System.Action<int> OnBoostUpdate;
    public System.Action<Transform, float> OnRealeasePressed;

    private Vector3 moveVelocity = Vector3.zero;
    private Vector3 envVelocity = Vector3.zero;
    private Vector3 desiredEnvVelocity = Vector3.zero;
    private Vector3 velocity = Vector3.zero;

    private int maxBoostCount;
    private int currentBoostCount;
    private float remainBoostCoolTime;
    private float pendingGain = 0;
    private float envScale = 1.0f;
    private Coroutine isHit;

    private HashSet<Collider> hitThisFarme = new();
    public bool isCutscene = false;

    //private void Update()
    //{
    //    if (isCutscene) return;
    //    if (isActive)
    //    {
    //        HandleStateLogic();
    //        Move();
    //        hitThisFarme.Clear();
    //    }
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Wall") && remainInvincibleTime <= 0)
    //    {
    //        //OnHitWall?.Invoke();
    //        //remainBulletTime -= bulletWallHit;
    //        //remainBulletTime = Mathf.Clamp(remainBulletTime, 0f, maxBulletTime);
    //        //remainInvincibleTime = invincibleTime;
    //    }
    //    else if (other.CompareTag("Ember"))
    //    {
    //        OnLightUp?.Invoke();

    //        //불씨를 먹으면 대시, 대시 중에 또 먹으면 하이퍼대시
    //        if (arrowState == ArrowState.None)
    //        {
    //            ChangeArrowState(ArrowState.Dash);
    //            Time.timeScale = 1;
    //            currentSpeed = dashSpeed;
    //            remainCoolTime = dashCoolTime;
    //        }
    //        else if (arrowState == ArrowState.HyperDash || arrowState == ArrowState.Dash)
    //        {
    //            ChangeArrowState(ArrowState.HyperDash);
    //            Time.timeScale = 1;
    //            currentSpeed = hyperDashSpeed;
    //            remainCoolTime = hyperDashCoolTime;
    //        }

    //        remainBulletTime += bulletTimeLightUp;
    //        remainBulletTime = Mathf.Clamp(remainBulletTime, 0f, maxBulletTime);
    //        Destroy(other.gameObject);
    //    }
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("WindArea"))
    //    {
    //        WindSetting windSetting = other.GetComponent<WindArea>().Wind;
    //        desiredEnvVelocity += windSetting.dir * windSetting.speed;
    //    }
    //    else if (other.CompareTag("DarkArea"))
    //    {
    //        TorchTrigger torchTrigger = other.GetComponent<TorchTrigger>();
    //        torchTrigger.Enter(this);
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("WindArea"))
    //    {
    //        WindSetting windSetting = other.GetComponent<WindArea>().Wind;
    //        desiredEnvVelocity -= windSetting.dir * windSetting.speed;
    //    }
    //    else if (other.CompareTag("DarkArea"))
    //    {
    //        TorchTrigger torchTrigger = other.GetComponent<TorchTrigger>();
    //        torchTrigger.Exit();
    //    }
    //    else if (other.CompareTag("Drop") && isHit == null)
    //    {
    //        //환경 속도 줄이기, 인풋 빼기
    //        isHit = StartCoroutine(hitCorutine());
    //    }
    //}

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hitThisFarme.Add(hit.collider))
            return;
        if (hit.collider.CompareTag("Ember"))
        {
            OnLightUp?.Invoke();
            //불씨 획득 이펙트 생성
            GameObject obj = Instantiate(emberGainEffect, transform.position, Quaternion.identity);
            obj.GetComponent<Week4ParticleAttractor>().SetTarget(transform, true);

            //불씨를 먹으면 대시, 대시 중에 또 먹으면 하이퍼대시
            if (arrowState == ArrowState.None)
            {
                ChangeArrowState(ArrowState.Dash);
                Time.timeScale = 1;
                currentSpeed = dashSpeed;
                remainCoolTime = dashCoolTime;
            }
            else if (arrowState == ArrowState.HyperDash || arrowState == ArrowState.Dash)
            {
                ChangeArrowState(ArrowState.HyperDash);
                Time.timeScale = 1;
                currentSpeed = hyperDashSpeed;
                remainCoolTime = hyperDashCoolTime;
            }

            //remainBulletTime += bulletTimeLightUp;
            //remainBulletTime = Mathf.Clamp(remainBulletTime, 0f, maxBulletTime);
            Destroy(hit.gameObject);
        }
        else if (hit.collider.CompareTag("Wisp"))
        {
            boostCountUp();
            Destroy(hit.gameObject);
        }
    }

    //public void Initialize()
    //{
    //    currentSpeed = speed;
    //    remainCoolTime = 0;
    //    remainInvincibleTime = 0;
    //    remainBulletTime = startBulletTime;
    //    arrowState = ArrowState.None;
    //    brainTransform = FindFirstObjectByType<CinemachineBrain>().transform;
    //    characterController = GetComponent<CharacterController>();
    //    maxBoostCount = startBoostCount;
    //    currentBoostCount = startBoostCount;
    //    remainBoostCoolTime = boostCoolTime;
    //    OnBoostUpdate?.Invoke(currentBoostCount);
    //}

    //public void inputInit(InputManager inputManager)
    //{
    //    input = inputManager;
    //}

    public void GameStart()
    {
        isActive = true;
    }

    public void GameOver()
    {
        ChangeArrowState(ArrowState.None);
        Time.timeScale = 1f;
        isActive = false;
    }

    //클리어 시 조작만 멈춘다. (클리어 연출은 추후)
    public void GameClear()
    {
        ChangeArrowState(ArrowState.None);
        Time.timeScale = 1f;
        isActive = false;
    }

    private void ChangeArrowState(ArrowState state)
    {
        arrowState = state;
        OnArrowStateChange?.Invoke(arrowState);
    }

    //private void HandleStateLogic() //상태 변환 및 그에 따른 변수도 조금 바꿔주자
    //{
    //    if (arrowState == ArrowState.None)
    //    {
    //        if (input.BulletTimePressed)
    //        {
    //            ChangeArrowState(ArrowState.BulletTime);
    //            currentSpeed = bulletTimeSpeed;
    //            Time.timeScale = bulletTimeScale;
    //        }
    //        else if (input.BoostPressed && currentBoostCount > 0)
    //        {
    //            //current 부스트 감소 
    //            currentBoostCount--;
    //            OnBoostUpdate?.Invoke(currentBoostCount);
    //            remainBoostCoolTime = boostCoolTime;
    //            //대시 전환
    //            ChangeArrowState(ArrowState.Dash);
    //            Time.timeScale = 1;
    //            currentSpeed = dashSpeed;
    //            remainCoolTime = dashCoolTime;
    //        }
    //        //바로 위에서 넣은 bulletTimeSpeed를 덮어쓴다. (불릿타임 속도가 적용되지 않음)
    //        //currentSpeed = speed;
    //    }
    //    else if (arrowState == ArrowState.Dash)
    //    {
    //        if (input.BoostPressed && currentBoostCount > 0)
    //        {
    //            //current 부스트 감소 
    //            currentBoostCount--;
    //            OnBoostUpdate?.Invoke(currentBoostCount);
    //            remainBoostCoolTime = boostCoolTime;
    //            //대시 전환
    //            ChangeArrowState(ArrowState.HyperDash);
    //            Time.timeScale = 1;
    //            currentSpeed = hyperDashSpeed;
    //            remainCoolTime = hyperDashCoolTime;
    //        }
    //        //대시 시간이 끝나면 기본 상태로
    //        if (remainCoolTime <= 0)
    //        {
    //            ChangeArrowState(ArrowState.None);
    //            currentSpeed = speed;
    //            remainCoolTime = 0;
    //        }
    //        else
    //        {
    //            currentSpeed = dashSpeed;
    //        }
    //    }
    //    else if (arrowState == ArrowState.BulletTime)
    //    {
    //        //키를 떼거나 게이지가 바닥나면 해제
    //        if (input.BulletTimeReleased || remainBulletTime <= 0)
    //        {
    //            ChangeArrowState(ArrowState.None);
    //            currentSpeed = speed;
    //            Time.timeScale = 1;
    //        }
    //        remainBulletTime -= Time.deltaTime * bulletTimeDiscount * bulletTimeDuringDiscount;
    //        if (remainBulletTime <= 0)
    //        {
    //            remainBulletTime = 0;
    //        }
    //    }
    //    else if (arrowState == ArrowState.HyperDash)
    //    {
    //        //하이퍼대시 시간이 끝나면 기본 상태로
    //        if (remainCoolTime <= 0)
    //        {
    //            ChangeArrowState(ArrowState.None);
    //            currentSpeed = speed;
    //            remainCoolTime = 0;
    //        }
    //        else
    //        {
    //            currentSpeed = hyperDashSpeed;
    //        }
    //    }

    //    if (input.RealeasePressed)
    //    {
    //        OnRealeasePressed?.Invoke(transform, remainBulletTime);
    //    }
    //}

    private void Move()
    {
        if (remainCoolTime > 0)
        {
            remainCoolTime -= Time.deltaTime;
        }
        if (remainInvincibleTime > 0)
        {
            remainInvincibleTime -= Time.deltaTime;
        }
        remainBulletTime -= Time.deltaTime * bulletTimeDiscount;

        //시간이 지날수록 난이도 상승 (대시 속도들도 같은 비율로)
        speed += difficultSpeedUp * Time.deltaTime;
        dashSpeed += difficultSpeedUp * Time.deltaTime * dashSpeed / speed;
        hyperDashSpeed += difficultSpeedUp * Time.deltaTime * hyperDashSpeed / speed;

        //게이지가 많을수록 빠르게 (0.33 ~ 0.66배)
        float trailRangeRatio = remainBulletTime / maxBulletTime;
        trailRangeRatio = Mathf.Clamp(trailRangeRatio, 0.33f, 0.66f);

        //카메라가 보는 방향 기준 WASD 이동, 바닥과 벽은 CharacterController가 콜라이더로 막는다.
        //UpdateVelocity(currentSpeed * trailRangeRatio);
        envVelocity = Vector3.MoveTowards(envVelocity, desiredEnvVelocity * envScale, envAccel * Time.deltaTime);
        velocity = envVelocity + moveVelocity;
        //이제 여기서 환경 값을 더해준 것으로 움직인다.
        characterController.Move(velocity * Time.deltaTime);

        Vector3 horizontalMove = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        if (horizontalMove.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontalMove);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        velocity = characterController.velocity; //벽에 막힌 만큼 속도에도 반영
                                                 //moveVelocity = characterController.velocity - envVelocity; // 벽에 막힌 만큼 입력 속도에서 제거

        if (velocity.sqrMagnitude < 0.0001f)
        {
            if (!isIdleCircling)
            {
                Vector3 currentHeading = transform.forward;
                currentHeading.y = 0f;
                if (currentHeading.sqrMagnitude < 0.01f)
                {
                    currentHeading = brainTransform.forward;
                }
                currentHeading.Normalize();

                Vector3 rightOffset = Vector3.Cross(Vector3.up, currentHeading).normalized * idleCircleRadius;
                idleCenterPosition = transform.position + rightOffset;

                Vector3 fromCenter = transform.position - idleCenterPosition;
                idleCurrentAngle = Mathf.Atan2(fromCenter.z, fromCenter.x);
                isIdleCircling = true;

            }
            idleCurrentAngle += idleCircleSpeed * Time.deltaTime;

            float targetX = idleCenterPosition.x + Mathf.Cos(idleCurrentAngle) * idleCircleRadius;
            float targetZ = idleCenterPosition.z + Mathf.Sin(idleCurrentAngle) * idleCircleRadius;
            Vector3 targetPos = new Vector3(targetX, transform.position.y, targetZ);

            Vector3 desiredIdleVelocity = (targetPos - transform.position) / Time.deltaTime;
            moveVelocity = Vector3.Lerp(moveVelocity, desiredIdleVelocity, idleCircleEnterLerp * Time.deltaTime);
            return;
        }
        isIdleCircling = false;

        trail.time = remainBulletTime; //남은 게이지만큼 트레일 길이

        if (remainBulletTime <= 0)
        {
            OnGameOver?.Invoke();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            GameOver();
        }

        if (pendingGain != 0f)
        {
            float step = Mathf.MoveTowards(0f, pendingGain, gainPerSecond * Time.deltaTime);
            pendingGain -= step;
            remainBulletTime = Mathf.Clamp(remainBulletTime + step, 0f, maxBulletTime);
        }

        //부스트 사용 채워 주기
        if (remainBoostCoolTime > 0)
        {
            remainBoostCoolTime -= Time.deltaTime;
            if (remainBoostCoolTime <= 0.01)
            {
                if (currentBoostCount < maxBoostCount)
                {
                    currentBoostCount++;
                    OnBoostUpdate?.Invoke(currentBoostCount);
                }
                if (currentBoostCount == maxBoostCount)
                {
                    remainBoostCoolTime = 0f;
                }
                else
                {
                    remainBoostCoolTime = boostCoolTime;
                }
            }
        }
    }
    //private void UpdateVelocity(float maxSpeed)
    //{
    //    Vector2 moveInput = input.Move;
    //    Vector3 moveDir = brainTransform.forward * moveInput.y + brainTransform.right * moveInput.x;
    //    if (input.UpPressing && !input.DownPressing)
    //    {
    //        moveDir += brainTransform.up * input.UpInput * verticalRatio;
    //    }
    //    else if (!input.UpPressing && input.DownPressing)
    //    {
    //        moveDir -= brainTransform.up * input.DownInput * verticalRatio;
    //    }
    //    //입력이 없으면 마찰처럼 감속 -> 입력이 없으면 원 운동

    //    moveDir.Normalize();

    //    Vector3 along = Vector3.Project(moveVelocity, moveDir);        //입력 방향 성분
    //    Vector3 side = Vector3.ProjectOnPlane(moveVelocity, moveDir); //옆 성분

    //    side = Vector3.MoveTowards(side, Vector3.zero, decel * Time.deltaTime);         //브레이크
    //    along = Vector3.MoveTowards(along, moveDir * maxSpeed, accel * Time.deltaTime);  //엑셀

    //    moveVelocity = along + side;

    //}

    [ContextMenu("부스트 개수 증가")]
    public void boostCountUp()
    {
        maxBoostCount++;
        currentBoostCount = maxBoostCount;
        OnBoostUpdate?.Invoke(currentBoostCount);
        Debug.Log(maxBoostCount);
        remainBoostCoolTime = 0f;
    }

    public void remainBulletTimeGain(float gain)
    {
        pendingGain += gain;
    }

    //public bool TryLendWisp(out Pose from)
    //{
    //    from = default;
    //    if (!isActive || currentBoostCount <= 0)
    //        return false;

    //    from = wispTail.GetArrowPose(currentBoostCount - 1); //이번에 꺼질 마지막 꼬리
    //    currentBoostCount--;
    //    maxBoostCount--;
    //    OnBoostUpdate?.Invoke(currentBoostCount);            //여기서 그 꼬리가 SetActive(false)
    //    return true;
    //}

    public void ReturnWisps(int count)
    {
        if (count <= 0)
            return;
        maxBoostCount += count;
        currentBoostCount += count;
        OnBoostUpdate?.Invoke(currentBoostCount);
    }

    ////4초 내에 두번 맞는 개폐급이 있을까?
    //public IEnumerator hitCorutine()
    //{
    //    const float damageTime = 1.0f;
    //    const float healTime = 3.0f;
    //    const float ratio = 0.2f;

    //    input.PlayerDisable();
    //    envVelocity *= ratio;   // 맞는 순간 즉시 감속 (타격감)

    //    float count = 0f;
    //    while (count < damageTime)
    //    {
    //        count += Time.deltaTime;
    //        envScale = Mathf.Lerp(1f, ratio, count / damageTime);
    //        yield return null;
    //    }

    //    if (isActive)
    //        input.PlayerEnable();

    //    count = 0f;
    //    while (count < healTime)
    //    {
    //        count += Time.deltaTime;
    //        envScale = Mathf.Lerp(ratio, 1f, count / healTime);
    //        yield return null;
    //    }

    //    envScale = 1f;
    //    isHit = null;
    //}

    public int GetBoostCount()
    {
        return maxBoostCount;
    }

    public void ResetMovementState(Vector3 newPosition)
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        transform.position = newPosition;

        moveVelocity = Vector3.zero;
        envVelocity = Vector3.zero;
        desiredEnvVelocity = Vector3.zero;
        velocity = Vector3.zero;

        isIdleCircling = false;
        idleCenterPosition = newPosition;
        idleCurrentAngle = 0f;

        if (trail != null)
            trail.Clear();

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = true;
    }

    public void CutChunsik()
    {
        maxBoostCount = 0;
        currentBoostCount = 0;
        OnBoostUpdate?.Invoke(currentBoostCount);
    }
}
