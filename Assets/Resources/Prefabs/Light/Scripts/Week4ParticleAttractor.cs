using System.Collections.Generic;
using UnityEngine;

public class Week4ParticleAttractor : MonoBehaviour
{
    [SerializeField] private float delayTime = 1;
    [SerializeField] public float speed = 50f;
    [SerializeField] private Transform targetTransform;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private float timer = 0;
    private ArrowController ac;
    private float particleGain;
    private Dictionary<uint, Vector3> fixedTargetPositions = new Dictionary<uint, Vector3>();

    public bool isTargetPlayer = true;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
        ac = FindAnyObjectByType<ArrowController>();
        ParticleSystem.Burst burst = ps.emission.GetBurst(0);
        particleGain = ac.BulletTimeLightUp / burst.count.constant;
    }


    void Update()
    {
        timer += Time.deltaTime;
        if (timer <= delayTime) return;
        int numParticlesAlive = ps.GetParticles(particles);
        if (numParticlesAlive == 0) return;

        for (int i = 0; i < numParticlesAlive; i++)
        {
            Vector3 currentTarget;
            if (isTargetPlayer)
            {
                currentTarget = targetTransform.position;
            }
            else
            {
                uint seed = particles[i].randomSeed;
                if (!fixedTargetPositions.TryGetValue(seed, out currentTarget))
                {
                    currentTarget = targetTransform.position;
                    currentTarget.x += Random.Range(-750, 7500);
                    currentTarget.z += Random.Range(-1500, 1500);
                    fixedTargetPositions[seed] = currentTarget;
                }
                speed = 750;
            }
            particles[i].position = Vector3.MoveTowards(particles[i].position, currentTarget, speed * Time.deltaTime);

            if (Vector3.Distance(particles[i].position, currentTarget) < 0.3f && targetTransform == ac.GetComponent<Transform>())
            {
                ac.remainBulletTimeGain(particleGain);
                particles[i].remainingLifetime = 0f;
                fixedTargetPositions.Remove(particles[i].randomSeed);
                if (numParticlesAlive < 5f)
                {
                    Destroy(gameObject, 5f);
                }

            }
        }
        ps.SetParticles(particles, numParticlesAlive);

    }

    void OnEnable()
    {
        timer = 0;
    }

    public void SetTarget(Transform transform, bool target)
    {
        targetTransform = transform;
        isTargetPlayer = target;
    }
}
