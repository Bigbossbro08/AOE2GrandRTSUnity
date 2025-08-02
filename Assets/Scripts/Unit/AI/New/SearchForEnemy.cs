using System.Collections.Generic;
using UnityEngine;
using static BasicAttackAIModule;

namespace CoreGameUnitAI
{
    public class SearchForEnemy
    {
        private float lineOfSight = 5.0f;
        private float timer;
        private float maxTime = 2f;
        private MovableUnit self = null;

        public SearchForEnemy(MovableUnit self, float lineOfSight = 5.0f, float maxTime = 0.5f) {
            this.timer = 0.0f;
            this.lineOfSight = lineOfSight;
            this.maxTime = maxTime;
            this.self = self;
        }

        public void Reset()
        {
            timer = 0f;
        }

        public bool Update(float dt, out MovableUnit returnUnit)
        {
            timer += dt;
            if (TimedOut)
            {
                timer = 0.0f;

                // scan area logic
                return FoundEnemy(self.transform.position, out returnUnit);
            }
            returnUnit = null;
            return false;
        }

        public bool IsValidEnemyTarget(MovableUnit unit)
        {
            if (unit == self) return false;
            if (!StatComponent.IsUnitAliveOrValid(unit)) return false;
            if (unit.playerId == self.playerId) return false;
            return true;
        }

        public bool FoundEnemy(Vector3 position, out MovableUnit target)
        {
            target = null;
            List<Unit> units = UnitManager.Instance.spatialHashGrid.QueryInRadius(position, lineOfSight);
            MinHeap<HeapUnitNode> unitHeap = new MinHeap<HeapUnitNode>();

            // Finding enemy logic
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit.GetType() != typeof(MovableUnit)) continue;
                if (IsValidEnemyTarget((MovableUnit)unit))
                {
                    float sqrDistance = (self.transform.position - unit.transform.position).sqrMagnitude;
                    unitHeap.Push(new HeapUnitNode(i, sqrDistance));
                }
            }
            if (unitHeap.Count > 0)
            {
                HeapUnitNode heapUnitNode = unitHeap.Pop();
                Unit targetUnit = units[heapUnitNode.Index];
                Debug.Assert(targetUnit != null);
                Debug.Assert(targetUnit.GetType() == typeof(MovableUnit));
                target = (MovableUnit)targetUnit;
            }

            return target != null;
        }

        public bool TimedOut => timer > maxTime;
    }
}
