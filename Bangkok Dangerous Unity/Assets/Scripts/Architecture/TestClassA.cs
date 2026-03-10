using UnityEngine;

namespace GameArchitecture
{
    public class TestClassA : MonoBehaviour, IReference
    {
        private TestClassB TestClassB;

        public void Register()
        {
            ReferenceManager.AddReference_Self(gameObject, this);
        }

        public void Setup()
        {
            ReferenceManager.TryGetReference_Children<TestClassB>(gameObject, out TestClassB);
        }

        public void Start()
        {
            Debug.Log(TestClassB.name);
        }
    }
}