using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace CityPeople
{
    public class CityPeople : MonoBehaviour
    {
        [Header("Visual & Animation Settings")]
        [SerializeField] private bool AutoPlayAnimations = true;
        [SerializeField] private Material PaletteOverride;

        [Header("Walking Settings")]
        public Transform[] walkPoints;
        private int currentPoint = 0;
        private NavMeshAgent agent;

        private AnimationClip[] myClips;
        private Animator animator;
        public string CurrentPaletteName { get; private set; }
        public const string people_pal_prefix = "people_pal";
        private List<Renderer> _paletteMeshes;

        private void Awake()
        {
            var AllRenderers = gameObject.GetComponentsInChildren<Renderer>();
            _paletteMeshes = new List<Renderer>();
            foreach (Renderer r in AllRenderers)
            {
                var matName = r.sharedMaterial.name;
                var len = Math.Min(people_pal_prefix.Length, matName.Length);
                if (matName.Substring(0, len) == people_pal_prefix)
                {
                    _paletteMeshes.Add(r);
                }
            }
            if (_paletteMeshes.Count > 0)
            {
                CurrentPaletteName = _paletteMeshes[0].sharedMaterial.name;
            }

            if (PaletteOverride != null)
            {
                SetPalette(PaletteOverride);
            }
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

            if (animator != null)
            {
                myClips = animator.runtimeAnimatorController.animationClips;
            }

            if (AutoPlayAnimations)
            {
                // Collider for interaction
                CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.8f, 0f);
                collider.radius = 0.3f;
                collider.height = 1.77f;
                collider.direction = 1;
            }

            if (walkPoints.Length > 0 && agent != null)
            {
                GotoNextPoint();
            }

            StartCoroutine(AnimationLogic());
        }

        private void Update()
        {
            if (agent != null && !agent.pathPending && agent.remainingDistance < 0.5f)
            {
                GotoNextPoint();
            }

            // Update walk animation
            if (animator != null)
            {
                bool isWalking = agent.velocity.magnitude > 0.1f;
                animator.SetBool("isWalking", isWalking);
            }
        }

        private void GotoNextPoint()
        {
            if (walkPoints.Length == 0) return;

            agent.destination = walkPoints[currentPoint].position;
            currentPoint = (currentPoint + 1) % walkPoints.Length;
        }

        public void SetPalette(Material mat)
        {
            if (mat != null)
            {
                if (mat.name.StartsWith(people_pal_prefix))
                {
                    CurrentPaletteName = mat.name;
                    foreach (Renderer r in _paletteMeshes)
                    {
                        r.material = mat;
                    }
                }
                else
                {
                    Debug.Log("Material name should start with 'people_pal...' by convention.");
                }
            }
        }

        private IEnumerator AnimationLogic()
        {
            while (true)
            {
                if (AutoPlayAnimations && agent.velocity.magnitude < 0.1f)
                {
                    PlayAnyClip();
                }
                yield return new WaitForSeconds(15.0f + Random.value * 5.0f);
            }
        }

        public void PlayAnyClip()
        {
            if (myClips == null || myClips.Length == 0) return;
            var cl = myClips[Random.Range(0, myClips.Length)];
            animator.CrossFadeInFixedTime(cl.name, 1.0f, -1, Random.value * cl.length);
        }
    }
}
