﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPS.Model
{
    public class Collaborator : User
    {
        public virtual decimal Salary { get; set; }

        public virtual Parking Parking { get; set; }
    }
}

