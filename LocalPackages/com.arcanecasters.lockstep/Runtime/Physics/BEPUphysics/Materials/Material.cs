using FixMath.NET;
using System;

namespace BEPUphysics.Materials
{
    ///<summary>
    /// Material properties for collidable objects.
    ///</summary>
    public class Material
    {
        // 전투의 매 틱 속도 요청에는 이전 틱 충격량과 겹침 보정을 누적하지 않는다.
        // 양쪽 재질이 켠 접촉에만 적용하며 기본 엔진 동작은 유지한다.
        public bool VelocityOnlyContactResponse { get; set; }
        public Fix64 MaximumContactImpulse { get; set; } = Fix64.MaxValue;
        internal Fix64 kineticFriction = MaterialManager.DefaultKineticFriction;
        ///<summary>
        /// Gets or sets the friction coefficient used when the object is sliding quickly and
        /// no special material relationship is defined between the colliding objects.
        ///</summary>
        public Fix64 KineticFriction
        {
            get
            {
                return kineticFriction;
            }
            set
            {
                kineticFriction = value;
                if (MaterialChanged != null)
                    MaterialChanged(this);
            }
        }

        internal Fix64 staticFriction = MaterialManager.DefaultStaticFriction;
        ///<summary>
        /// Gets or sets the friction coefficient used when the object is sliding slowly and
        /// no special material relationship is defined between the colliding objects.
        ///</summary>
        public Fix64 StaticFriction
        {
            get
            {
                return staticFriction;
            }
            set
            {
                staticFriction = value;
                if (MaterialChanged != null)
                    MaterialChanged(this);
            }
        }


        internal Fix64 bounciness = MaterialManager.DefaultBounciness;
        ///<summary>
        /// Gets or sets the coefficient of restitution between the objects when
        /// no special material relationship is defined between the colliding objects.
        ///</summary>
        public Fix64 Bounciness
        {
            get
            {
                return bounciness;
            }
            set
            {
                bounciness = value;
                if (MaterialChanged != null)
                    MaterialChanged(this);
            }
        }

        ///<summary>
        /// Gets or sets user data associated with the material.
        ///</summary>
        public object Tag { get; set; }
        int hashCode;
        ///<summary>
        /// Constructs a new material.
        ///</summary>
        public Material()
        {
            hashCode = (int)(((uint)GetHashCode()) * 19999999);
        }

        ///<summary>
        /// Constructs a new material.
        ///</summary>
        ///<param name="staticFriction">Static friction to use.</param>
        ///<param name="kineticFriction">Kinetic friction to use.</param>
        ///<param name="bounciness">Bounciness to use.</param>
        public Material(Fix64 staticFriction, Fix64 kineticFriction, Fix64 bounciness)
            : this()
        {
            this.staticFriction = staticFriction;
            this.kineticFriction = kineticFriction;
            this.bounciness = bounciness;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return hashCode;
        }

        ///<summary>
        /// Fires when the material properties change.
        ///</summary>
        public event Action<Material> MaterialChanged;

    }
}
