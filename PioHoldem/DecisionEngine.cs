﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PioHoldem
{
    abstract class DecisionEngine
    {
        public DecisionEngine()
        {
            
        }

        public abstract int GetAction();
    }
}
