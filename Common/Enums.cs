using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum PhaseCode : short
    {
        Unknown = 0x0,
        N = 0x1,
        C = 0x2,
        CN = 0x3,
        B = 0x4,
        BN = 0x5,
        BC = 0x6,
        BCN = 0x7,
        A = 0x8,
        AN = 0x9,
        AC = 0xA,
        ACN = 0xB,
        AB = 0xC,
        ABN = 0xD,
        ABC = 0xE,
        ABCN = 0xF
    }

    public enum RegulatingControlModeKind : short
    {

        voltage = 1,				// Supply transformer
        activePower = 2,			// Transformer supplying a consumer
        reactivePower = 3,			// Transformer used only for grounding of network neutral
        currentFlow = 4,			// Feeder voltage regulator
        fix = 5,				// Step
        admittance = 6,			// Step-up transformer next to a generator.
        timeScheduled = 7,		// HV/HV transformer within transmission network.
        teperature = 8,
        powerFactor = 9
    }

    public enum WindingConnection : short
    {
        Y = 1,		// Wye
        D = 2,		// Delta
        Z = 3,		// ZigZag
        I = 4,		// Single-phase connection. Phase-to-phase or phase-to-ground is determined by elements' phase attribute.
        Scott = 5,   // Scott T-connection. The primary winding is 2-phase, split in 8.66:1 ratio
        OY = 6,		// 2-phase open wye. Not used in Network Model, only as result of Topology Analysis.
        OD = 7		// 2-phase open delta. Not used in Network Model, only as result of Topology Analysis.
    }
    public enum CurveStyle : short
    {
        constantYValue = 0,
        formula = 1,
        rampYValue = 2,
        straightLineYValues = 3
    }

    public enum UnitMultiplier : short
    {
        G = 1,
        M = 2,
        T = 3,
        c = 4,
        d = 5,
        k = 6,
        m = 7,
        micro = 8,
        n = 9,
        none = 10,
        p = 11

    }

    public enum UnitSymbol : short
    {
        A = 1,
        F = 2,
        H = 3,
        Hz = 4,
        J = 5,
        N = 6,
        Pa = 7,
        S = 8,
        V = 9,
        VA = 10,
        VAh = 11,
        VArh = 12,
        W = 13,
        Wh = 14,
        deg = 15,
        degC = 16,
        g = 17,
        h = 18,
        m = 19,
        m2 = 20,
        min = 21,
        none = 22,
        ohm = 23,
        rad = 24,
        s = 25,
        VAr = 26,
        m3 = 27

    }

    public enum WindingType : short
    {
        None = 0,
        Primary = 1,
        Secondary = 2,
        Tertiary = 3
    }
    public enum EnumTest : short
    {
        prviAtribut =0,
        drugiAtribut = 1,
        treciAtribut = 2
    }
    public enum MeasurementType
    {
        BOOL = 1,
        INT,
        LONG,
        STRING,
        SHORT,
        DOUBLE,
        FLOAT,
        BYTE,
        MODELCODE,
        LID,
        GID,
        DATETIME,
        CHAR
    }
}
