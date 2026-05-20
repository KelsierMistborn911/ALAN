using UnityEngine;

public interface IBehavior
{
    string BehaviorName { get; }
    void Initialize(PackMember member);
    void UpdateBehavior();
    void OnBehaviorActivated();
    void OnBehaviorDeactivated();
    bool IsActive { get; }
}
