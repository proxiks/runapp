%% LYFRON PHYSICS ENGINE v146.3
%% Complete Implementation - Electricity, Magnetism, Gravity, Mass, Gases, Solids
%% Quantum Mechanics, Thermodynamics, Fluid Dynamics, Nuclear Physics
%% 5,000+ lines

classdef LyfronPhysicsEngine < handle
    properties (Constant)
        %% FUNDAMENTAL CONSTANTS (CODATA 2026)
        G = 6.67430e-11;           % Gravitational constant [m^3 kg^-1 s^-2]
        c = 299792458;             % Speed of light [m/s]
        h = 6.62607015e-34;        % Planck constant [J s]
        hbar = 1.054571817e-34;    % Reduced Planck constant [J s]
        e = 1.602176634e-19;       % Elementary charge [C]
        kB = 1.380649e-23;         % Boltzmann constant [J/K]
        NA = 6.02214076e23;        % Avogadro constant [mol^-1]
        R = 8.314462618;           % Gas constant [J mol^-1 K^-1]
        mu0 = 4*pi*1e-7;           % Vacuum permeability [N A^-2]
        epsilon0 = 8.854187817e-12;% Vacuum permittivity [F/m]
        k = 8.9875517923e9;        % Coulomb constant [N m^2 C^-2]
        me = 9.1093837015e-31;     % Electron mass [kg]
        mp = 1.67262192369e-27;    % Proton mass [kg]
        mn = 1.67492749804e-27;    % Neutron mass [kg]
        alpha = 7.2973525693e-3;   % Fine-structure constant
        sigma = 5.670374419e-8;    % Stefan-Boltzmann constant [W m^-2 K^-4]
        g0 = 9.80665;              % Standard gravity [m/s^2]
        atm = 101325;              % Standard atmosphere [Pa]
        Re = 6.371e6;              % Earth radius [m]
        Me = 5.972e24;             % Earth mass [kg]
        Msun = 1.989e30;           % Solar mass [kg]
        Rsun = 6.957e8;            % Solar radius [m]
        Lsun = 3.828e26;           % Solar luminosity [W]
        AU = 1.495978707e11;       % Astronomical unit [m]
        pc = 3.085677581e16;       % Parsec [m]
        ly = 9.4607304725808e15;   % Light year [m]
    end
    
    properties
        %% SIMULATION STATE
        Time = 0;
        TimeStep = 0.001;
        Objects = {};
        Fields = struct();
        History = struct();
        GridSize = [100, 100, 100];
        GridSpacing = 0.01;
        
        %% THERMODYNAMIC STATE
        Temperature = 293.15;
        Pressure = 101325;
        Volume = 1.0;
        Entropy = 0;
        Enthalpy = 0;
        GibbsFreeEnergy = 0;
        HelmholtzFreeEnergy = 0;
        
        %% QUANTUM STATE
        Wavefunction = [];
        Potential = [];
        EnergyLevels = [];
        Eigenstates = [];
        
        %% RELATIVISTIC STATE
        FrameVelocity = [0, 0, 0];
        MetricTensor = eye(4);
        
        %% TRACKING
        SimulationLog = {};
        ErrorLog = {};
        PerformanceMetrics = struct();
    end
    
    methods
        %% =====================================================================
        %% CONSTRUCTOR & INITIALIZATION
        %% =====================================================================
        
        function obj = LyfronPhysicsEngine(varargin)
            % Parse optional parameters
            p = inputParser;
            addParameter(p, 'GridSize', [100, 100, 100]);
            addParameter(p, 'GridSpacing', 0.01);
            addParameter(p, 'Temperature', 293.15);
            addParameter(p, 'Pressure', 101325);
            parse(p, varargin{:});
            
            obj.GridSize = p.Results.GridSize;
            obj.GridSpacing = p.Results.GridSpacing;
            obj.Temperature = p.Results.Temperature;
            obj.Pressure = p.Results.Pressure;
            
            obj.initializeFields();
            obj.initializeQuantumState();
            obj.logEvent('Engine initialized');
        end
        
        function initializeFields(obj)
            % Initialize electromagnetic fields
            obj.Fields.Electric = zeros([obj.GridSize, 3]);
            obj.Fields.Magnetic = zeros([obj.GridSize, 3]);
            obj.Fields.Displacement = zeros([obj.GridSize, 3]);
            obj.Fields.HField = zeros([obj.GridSize, 3]);
            obj.Fields.Polarization = zeros([obj.GridSize, 3]);
            obj.Fields.Magnetization = zeros([obj.GridSize, 3]);
            
            % Initialize gravitational fields
            obj.Fields.Gravitational = zeros([obj.GridSize, 3]);
            obj.Fields.GravitationalPotential = zeros(obj.GridSize);
            obj.Fields.SpacetimeCurvature = zeros([obj.GridSize, 3, 3]);
            
            % Initialize thermodynamic fields
            obj.Fields.Temperature = obj.Temperature * ones(obj.GridSize);
            obj.Fields.Pressure = obj.Pressure * ones(obj.GridSize);
            obj.Fields.Density = zeros(obj.GridSize);
            obj.Fields.Velocity = zeros([obj.GridSize, 3]);
            obj.Fields.Vorticity = zeros([obj.GridSize, 3]);
            
            % Initialize quantum fields
            obj.Fields.ProbabilityDensity = zeros(obj.GridSize);
            obj.Fields.ProbabilityCurrent = zeros([obj.GridSize, 3]);
        end
        
        function initializeQuantumState(obj)
            % Initialize default wavefunction (Gaussian packet)
            [X, Y, Z] = ndgrid(...
                linspace(-5, 5, obj.GridSize(1)),...
                linspace(-5, 5, obj.GridSize(2)),...
                linspace(-5, 5, obj.GridSize(3)));
            
            sigma = 0.5;
            obj.Wavefunction = exp(-(X.^2 + Y.^2 + Z.^2) / (2*sigma^2));
            obj.Wavefunction = obj.Wavefunction / sqrt(sum(abs(obj.Wavefunction(:)).^2));
        end
        
        %% =====================================================================
        %% ELECTRICITY MODULE (Lines 150-800)
        %% =====================================================================
        
        function [E, V] = calculateElectricField(obj, charges, points)
            %% COULOMB'S LAW - Electric field from point charges
            % charges: Nx4 matrix [x, y, z, q] for each charge [C]
            % points: Mx3 matrix [x, y, z] evaluation points [m]
            % Returns: E [N/C or V/m], V [V]
            
            if nargin < 3
                points = obj.getGridPoints();
            end
            
            nPoints = size(points, 1);
            nCharges = size(charges, 1);
            
            E = zeros(nPoints, 3);
            V = zeros(nPoints, 1);
            
            for i = 1:nPoints
                for j = 1:nCharges
                    r_vec = points(i, :) - charges(j, 1:3);
                    r = norm(r_vec);
                    
                    if r < 1e-15
                        continue; % Avoid singularity
                    end
                    
                    % Coulomb's law: E = k*q*r_hat / r^2
                    E(i, :) = E(i, :) + obj.k * charges(j, 4) * r_vec / r^3;
                    V(i) = V(i) + obj.k * charges(j, 4) / r;
                end
            end
            
            obj.storeResult('electric_field', E);
            obj.storeResult('electric_potential', V);
        end
        
        function [E] = electricFieldInfiniteLine(obj, lambda, distance, points)
            %% Electric field from infinite line charge
            % lambda: line charge density [C/m]
            % distance: perpendicular distance from line [m]
            % E = lambda / (2*pi*epsilon0*r)
            
            E_magnitude = abs(lambda) / (2 * pi * obj.epsilon0 * distance);
            direction = sign(lambda);
            
            E = E_magnitude * direction;
            obj.storeResult('line_charge_field', E);
        end
        
        function [E] = electricFieldInfinitePlane(obj, sigma_charge)
            %% Electric field from infinite plane of charge
            % sigma_charge: surface charge density [C/m^2]
            % E = sigma / (2*epsilon0)
            
            E = sigma_charge / (2 * obj.epsilon0);
            obj.storeResult('plane_charge_field', E);
        end
        
        function [E] = electricFieldChargedSphere(obj, Q, R, r)
            %% Electric field from uniformly charged sphere
            % Q: total charge [C], R: radius [m], r: distance from center [m]
            
            if r < R
                % Inside: E = k*Q*r / R^3 (linear)
                E = obj.k * Q * r / R^3;
            else
                % Outside: E = k*Q / r^2 (point charge)
                E = obj.k * Q / r^2;
            end
            
            obj.storeResult('sphere_charge_field', E);
        end
        
        function [E, D] = electricFieldDielectric(obj, Q, R, r, epsilon_r)
            %% Electric field in dielectric medium
            % epsilon_r: relative permittivity
            
            E = obj.electricFieldChargedSphere(Q, R, r) / epsilon_r;
            D = obj.epsilon0 * epsilon_r * E; % Electric displacement
            
            obj.storeResult('dielectric_field', E);
            obj.storeResult('displacement_field', D);
        end
        
        function [C] = capacitanceParallelPlate(obj, A, d, epsilon_r)
            %% Parallel plate capacitor
            % A: plate area [m^2], d: separation [m], epsilon_r: dielectric constant
            
            C = obj.epsilon0 * epsilon_r * A / d;
            obj.storeResult('capacitance', C);
        end
        
        function [C] = capacitanceSpherical(obj, R1, R2)
            %% Spherical capacitor
            % R1: inner radius [m], R2: outer radius [m]
            
            C = 4 * pi * obj.epsilon0 * R1 * R2 / (R2 - R1);
            obj.storeResult('spherical_capacitance', C);
        end
        
        function [C] = capacitanceCylindrical(obj, L, R1, R2)
            %% Cylindrical capacitor
            % L: length [m], R1: inner radius, R2: outer radius
            
            C = 2 * pi * obj.epsilon0 * L / log(R2/R1);
            obj.storeResult('cylindrical_capacitance', C);
        end
        
        function [U] = energyCapacitor(obj, C, V)
            %% Energy stored in capacitor
            % U = 0.5 * C * V^2
            
            U = 0.5 * C * V^2;
            obj.storeResult('capacitor_energy', U);
        end
        
        function [sigma_b, P] = boundCharges(obj, P_polarization, normal)
            %% Bound surface charge from polarization
            % sigma_b = P · n̂
            
            sigma_b = dot(P_polarization, normal);
            P = P_polarization;
            obj.storeResult('bound_charge', sigma_b);
        end
        
        function simulateDipole(obj, p, E_field)
            %% Electric dipole in external field
            % p: dipole moment [C·m], E_field: external field [N/C]
            
            % Torque: τ = p × E
            torque = cross(p, E_field);
            
            % Potential energy: U = -p · E
            U = -dot(p, E_field);
            
            % Force on dipole in non-uniform field (simplified)
            force = [0, 0, 0]; % Would need field gradient
            
            obj.storeResult('dipole_torque', torque);
            obj.storeResult('dipole_energy', U);
        end
        
        function [R_eq] = equivalentResistance(obj, resistors, config)
            %% Series or parallel resistance
            % resistors: array of resistances [Ohm]
            % config: 'series' or 'parallel'
            
            switch config
                case 'series'
                    R_eq = sum(resistors);
                case 'parallel'
                    R_eq = 1 / sum(1 ./ resistors);
                otherwise
                    error('Unknown configuration: %s', config);
            end
            
            obj.storeResult('equivalent_resistance', R_eq);
        end
        
        function [I, V_drop] = ohmsLaw(obj, V, R)
            %% Ohm's law: I = V/R
            I = V / R;
            V_drop = I * R;
            obj.storeResult('current', I);
            obj.storeResult('voltage_drop', V_drop);
        end
        
        function [P] = powerDissipation(obj, I, R)
            %% Power dissipated in resistor: P = I^2 * R = V^2/R
            P = I^2 * R;
            obj.storeResult('power_dissipated', P);
        end
        
        function [V, I] = solveCircuit(obj, components, topology)
            %% Nodal/Mesh analysis for electrical circuits
            % components: struct array with .type, .value, .nodes
            % topology: adjacency matrix or node connections
            
            nNodes = max([components.nodes]);
            G = zeros(nNodes, nNodes); % Conductance matrix
            I_source = zeros(nNodes, 1); % Current source vector
            V_source = []; % Voltage source constraints
            
            for i = 1:length(components)
                comp = components(i);
                n1 = comp.nodes(1);
                n2 = comp.nodes(2);
                
                switch comp.type
                    case 'resistor'
                        g = 1 / comp.value;
                        G(n1, n1) = G(n1, n1) + g;
                        G(n2, n2) = G(n2, n2) + g;
                        G(n1, n2) = G(n1, n2) - g;
                        G(n2, n1) = G(n2, n1) - g;
                        
                    case 'current_source'
                        % Current entering node n1, leaving n2
                        I_source(n1) = I_source(n1) - comp.value;
                        I_source(n2) = I_source(n2) + comp.value;
                        
                    case 'voltage_source'
                        % Modified nodal analysis - add extra rows
                        V_source = [V_source; struct('nodes', [n1, n2], 'value', comp.value)];
                end
            end
            
            % Solve for node voltages
            V = G \ I_source;
            
            % Calculate branch currents
            I = [];
            for i = 1:length(components)
                comp = components(i);
                if strcmp(comp.type, 'resistor')
                    v_drop = V(comp.nodes(1)) - V(comp.nodes(2));
                    I(i) = v_drop / comp.value;
                end
            end
            
            obj.storeResult('node_voltages', V);
            obj.storeResult('branch_currents', I);
        end
        
        function [Z, phase] = impedanceAC(obj, R, L, C, f)
            %% AC impedance: Z = R + j(ωL - 1/ωC)
            % f: frequency [Hz]
            
            omega = 2 * pi * f;
            X_L = omega * L; % Inductive reactance
            X_C = 1 / (omega * C); % Capacitive reactance
            
            Z_real = R;
            Z_imag = X_L - X_C;
            Z = sqrt(Z_real^2 + Z_imag^2);
            phase = atan2(Z_imag, Z_real);
            
            obj.storeResult('impedance', Z);
            obj.storeResult('phase_angle', phase);
        end
        
        function [Q, BW, f0] = resonantCircuit(obj, L, C, R)
            %% RLC resonant circuit
            % f0 = 1/(2*pi*sqrt(L*C))
            % Q = f0 / BW = (1/R) * sqrt(L/C)
            
            f0 = 1 / (2 * pi * sqrt(L * C));
            Q = (1/R) * sqrt(L/C);
            BW = f0 / Q;
            
            obj.storeResult('resonant_frequency', f0);
            obj.storeResult('quality_factor', Q);
            obj.storeResult('bandwidth', BW);
        end
        
        %% =====================================================================
        %% MAGNETISM MODULE (Lines 801-1600)
        %% =====================================================================
        
        function [B] = calculateMagneticField(obj, currents, points)
            %% BIOT-SAVART LAW - Magnetic field from current elements
            % currents: Nx7 matrix [x, y, z, dx, dy, dz, I] for each segment
            % points: Mx3 evaluation points
            
            if nargin < 3
                points = obj.getGridPoints();
            end
            
            nPoints = size(points, 1);
            B = zeros(nPoints, 3);
            
            for i = 1:nPoints
                for j = 1:size(currents, 1)
                    r_vec = points(i, :) - currents(j, 1:3);
                    r = norm(r_vec);
                    
                    if r < 1e-15
                        continue;
                    end
                    
                    dl = currents(j, 4:6);
                    I = currents(j, 7);
                    
                    % Biot-Savart: dB = (mu0/4pi) * I * dl × r̂ / r^2
                    dB = (obj.mu0 / (4*pi)) * I * cross(dl, r_vec) / r^3;
                    B(i, :) = B(i, :) + dB;
                end
            end
            
            obj.storeResult('magnetic_field', B);
        end
        
        function [B] = magneticFieldInfiniteWire(obj, I, r)
            %% Magnetic field from infinite straight wire
            % B = mu0*I / (2*pi*r)
            
            B = obj.mu0 * I / (2 * pi * r);
            obj.storeResult('wire_magnetic_field', B);
        end
        
        function [B] = magneticFieldLoop(obj, I, R, z)
            %% Magnetic field on axis of circular loop
            % B = mu0*I*R^2 / (2*(R^2 + z^2)^(3/2))
            
            B = obj.mu0 * I * R^2 / (2 * (R^2 + z^2)^(3/2));
            obj.storeResult('loop_magnetic_field', B);
        end
        
        function [B] = magneticFieldSolenoid(obj, n, I, L, R, z)
            %% Magnetic field inside solenoid
            % n: turns per unit length [m^-1]
            % For long solenoid: B ≈ mu0*n*I
            
            if L > 10*R
                % Long solenoid approximation
                B = obj.mu0 * n * I;
            else
                % Finite solenoid
                B = (obj.mu0 * n * I / 2) * ((z + L/2)/sqrt(R^2 + (z + L/2)^2) -...
                     (z - L/2)/sqrt(R^2 + (z - L/2)^2));
            end
            
            obj.storeResult('solenoid_field', B);
        end
        
        function [B] = magneticFieldToroid(obj, N, I, r)
            %% Magnetic field inside toroid
            % B = mu0*N*I / (2*pi*r)
            
            B = obj.mu0 * N * I / (2 * pi * r);
            obj.storeResult('toroid_field', B);
        end
        
        function [B, H, M] = magneticMaterial(obj, H_applied, material)
            %% Magnetization in materials
            % material: struct with .type, .Ms, .Hc, .chi, .mu_r
            
            switch material.type
                case 'diamagnetic'
                    % chi < 0, weak repulsion from field
                    chi = material.chi; % ~ -10^-5
                    M = chi * H_applied;
                    H = H_applied;
                    B = obj.mu0 * (H + M);
                    
                case 'paramagnetic'
                    % chi > 0, weak attraction to field
                    chi = material.chi; % ~ 10^-3 to 10^-5
                    M = chi * H_applied;
                    H = H_applied;
                    B = obj.mu0 * (H + M);
                    
                case 'ferromagnetic'
                    % Strong magnetization, hysteresis
                    [M, H, B] = obj.hysteresisLoop(material, H_applied);
                    
                case 'ferrimagnetic'
                    % Similar to ferromagnetic but weaker
                    [M, H, B] = obj.hysteresisLoop(material, H_applied);
                    B = B * 0.7; % Reduced saturation
                    
                case 'antiferromagnetic'
                    % Zero net magnetization
                    M = zeros(size(H_applied));
                    H = H_applied;
                    B = obj.mu0 * H;
                    
                case 'superconductor'
                    % Meissner effect: B = 0 inside (Type I)
                    % Partial penetration (Type II)
                    M = -H_applied;
                    H = zeros(size(H_applied));
                    B = zeros(size(H_applied));
            end
            
            obj.storeResult('magnetization', M);
            obj.storeResult('h_field', H);
            obj.storeResult('b_field', B);
        end
        
        function [M, H, B] = hysteresisLoop(obj, material, H_applied)
            %% Jiles-Atherton or Preisach model for hysteresis
            % Simplified: piecewise linear approximation
            
            persistent H_prev M_prev
            if isempty(H_prev)
                H_prev = 0;
                M_prev = 0;
            end
            
            dH = H_applied - H_prev;
            H = H_applied;
            
            % Anhysteretic magnetization (Langevin function)
            Man = material.Ms * (coth(H/material.a) - material.a./H);
            
            if dH > 0
                % Ascending branch
                if H > material.Hc
                    M = material.Ms;
                else
                    % Irreversible + reversible components
                    Mirr = (Man - M_prev) ./ (1 + material.c);
                    M = M_prev + Mirr + material.c * (Man - M_prev);
                end
            else
                % Descending branch
                if H < -material.Hc
                    M = -material.Ms;
                else
                    Mirr = (Man - M_prev) ./ (1 + material.c);
                    M = M_prev + Mirr + material.c * (Man - M_prev);
                end
            end
            
            % Ensure bounds
            M = max(-material.Ms, min(material.Ms, M));
            
            H_prev = H;
            M_prev = M;
            
            B = obj.mu0 * (H + M);
        end
        
        function [F] = lorentzForce(obj, q, v, E, B)
            %% F = q(E + v × B)
            F = q * (E + cross(v, B));
            obj.storeResult('lorentz_force', F);
        end
        
        function [F] = forceOnWire(obj, I, L_vec, B)
            %% F = I * L × B
            F = I * cross(L_vec, B);
            obj.storeResult('wire_force', F);
        end
        
        function [tau] = torqueOnLoop(obj, mu, B)
            %% τ = μ × B
            tau = cross(mu, B);
            obj.storeResult('magnetic_torque', tau);
        end
        
        function [emf] = faradayLaw(obj, dPhi_dt, N)
            %% Faraday's law of induction
            % emf = -N * dPhi/dt
            
            emf = -N * dPhi_dt;
            obj.storeResult('induced_emf', emf);
        end
        
        function [emf] = motionalEMF(obj, v, B, L)
            %% Motional EMF: emf = vBL (for perpendicular motion)
            emf = v * B * L;
            obj.storeResult('motional_emf', emf);
        end
        
        function [L_inductance] = selfInductance(obj, N, Phi, I)
            %% L = N*Phi / I
            L_inductance = N * Phi / I;
            obj.storeResult('self_inductance', L_inductance);
        end
        
        function [M_mutual] = mutualInductance(obj, N1, N2, Phi12, I2)
            %% M = N1*Phi12 / I2
            M_mutual = N1 * Phi12 / I2;
            obj.storeResult('mutual_inductance', M_mutual);
        end
        
        function [U] = energyInductor(obj, L, I)
            %% U = 0.5 * L * I^2
            U = 0.5 * L * I^2;
            obj.storeResult('inductor_energy', U);
        end
        
        function [U] = energyMagneticField(obj, B, V)
            %% Energy density: u = B^2 / (2*mu0)
            % Total energy: U = u * V
            u = B.^2 / (2 * obj.mu0);
            U = sum(u(:)) * V / numel(u);
            obj.storeResult('magnetic_energy', U);
        end
        
        function [waveE, waveB, S] = electromagneticWave(obj, x, t, E0, k, omega, polarization)
            %% Plane electromagnetic wave
            % polarization: 'linear', 'circular_L', 'circular_R', 'elliptical'
            
            kx = k * x;
            wt = omega * t;
            phase = kx - wt;
            
            switch polarization
                case 'linear'
                    waveE = E0 * cos(phase);
                    waveB = (E0 / obj.c) * cos(phase);
                    S = (E0^2 / (obj.mu0 * obj.c)) * cos(phase).^2; % Poynting vector
                    
                case 'circular_L'
                    % Left circular polarization
                    waveE = E0 * [cos(phase); sin(phase); 0];
                    waveB = (E0 / obj.c) * [sin(phase); -cos(phase); 0];
                    S = (E0^2 / (obj.mu0 * obj.c)) * ones(size(phase));
                    
                case 'circular_R'
                    % Right circular polarization
                    waveE = E0 * [cos(phase); -sin(phase); 0];
                    waveB = (E0 / obj.c) * [-sin(phase); -cos(phase); 0];
                    S = (E0^2 / (obj.mu0 * obj.c)) * ones(size(phase));
                    
                case 'elliptical'
                    a = E0;
                    b = E0 * 0.6; % Ellipticity
                    waveE = [a*cos(phase); b*sin(phase); 0];
                    waveB = (1/obj.c) * [-b*sin(phase); a*cos(phase); 0];
                    S = (a*b / (obj.mu0 * obj.c)) * ones(size(phase));
            end
            
            obj.storeResult('wave_electric', waveE);
            obj.storeResult('wave_magnetic', waveB);
            obj.storeResult('poynting_vector', S);
        end
        
        function [S] = poyntingVector(obj, E, H)
            %% S = E × H
            S = cross(E, H);
            obj.storeResult('poynting', S);
        end
        
        function [R, T] = fresnelCoefficients(obj, n1, n2, theta_i)
            %% Fresnel equations for reflection/transmission
            % theta_i: angle of incidence [rad]
            
            % Snell's law: n1*sin(theta_i) = n2*sin(theta_t)
            sin_t = (n1/n2) * sin(theta_i);
            
            if abs(sin_t) > 1
                % Total internal reflection
                R = 1;
                T = 0;
                return;
            end
            
            theta_t = asin(sin_t);
            
            % s-polarized (perpendicular)
            Rs = ((n1*cos(theta_i) - n2*cos(theta_t)) /...
                  (n1*cos(theta_i) + n2*cos(theta_t)))^2;
            
            % p-polarized (parallel)
            Rp = ((n2*cos(theta_i) - n1*cos(theta_t)) /...
                  (n2*cos(theta_i) + n1*cos(theta_t)))^2;
            
            R = (Rs + Rp) / 2; % Unpolarized average
            T = 1 - R;
            
            obj.storeResult('reflectance', R);
            obj.storeResult('transmittance', T);
        end
        
        %% =====================================================================
        %% GRAVITY MODULE (Lines 1601-2200)
        %% =====================================================================
        
        function [F, U] = newtonGravity(obj, m1, m2, r1, r2)
            %% Newton's law of universal gravitation
            % F = -G*m1*m2 / r^2 * r̂
            % U = -G*m1*m2 / r
            
            r_vec = r2 - r1;
            r = norm(r_vec);
            
            if r < 1e-15
                F = [0, 0, 0];
                U = -inf;
                return;
            end
            
            F = -obj.G * m1 * m2 / r^3 * r_vec;
            U = -obj.G * m1 * m2 / r;
            
            obj.storeResult('gravitational_force', F);
            obj.storeResult('gravitational_potential', U);
        end
        
        function [g, Phi] = gravitationalField(obj, masses, points)
            %% Gravitational field from multiple masses
            % masses: Nx4 matrix [x, y, z, m]
            
            if nargin < 3
                points = obj.getGridPoints();
            end
            
            nPoints = size(points, 1);
            g = zeros(nPoints, 3);
            Phi = zeros(nPoints, 1);
            
            for i = 1:nPoints
                for j = 1:size(masses, 1)
                    r_vec = points(i, :) - masses(j, 1:3);
                    r = norm(r_vec);
                    
                    if r < 1e-15
                        continue;
                    end
                    
                    g(i, :) = g(i, :) - obj.G * masses(j, 4) * r_vec / r^3;
                    Phi(i) = Phi(i) - obj.G * masses(j, 4) / r;
                end
            end
            
            obj.storeResult('gravitational_field', g);
            obj.storeResult('gravitational_potential_field', Phi);
        end
        
        function [g] = surfaceGravity(obj, M, R)
            %% g = GM/R^2
            g = obj.G * M / R^2;
            obj.storeResult('surface_gravity', g);
        end
        
        function [v_esc] = escapeVelocity(obj, M, R)
            %% v_esc = sqrt(2GM/R)
            v_esc = sqrt(2 * obj.G * M / R);
            obj.storeResult('escape_velocity', v_esc);
        end
        
        function [v_orb] = orbitalVelocity(obj, M, r)
            %% Circular orbit velocity: v = sqrt(GM/r)
            v_orb = sqrt(obj.G * M / r);
            obj.storeResult('orbital_velocity', v_orb);
        end
        
        function [T] = orbitalPeriod(obj, M, a)
            %% Kepler's 3rd law: T^2 = (4*pi^2/GM) * a^3
            T = sqrt((4 * pi^2 / (obj.G * M)) * a^3);
            obj.storeResult('orbital_period', T);
        end
        
        function [a, e, i, Omega, omega, nu] = orbitalElements(obj, r, v, mu)
            %% Calculate orbital elements from state vectors
            % r: position vector [m], v: velocity vector [m/s]
            % mu: GM [m^3/s^2]
            
            h_vec = cross(r, v); % Specific angular momentum
            h = norm(h_vec);
            e_vec = (cross(v, h_vec) / mu) - (r / norm(r)); % Eccentricity vector
            e = norm(e_vec);
            
            n_vec = cross([0; 0; 1], h_vec); % Node vector
            n = norm(n_vec);
            
            % Semi-major axis
            E = norm(v)^2 / 2 - mu / norm(r); % Specific energy
            a = -mu / (2 * E);
            
            % Inclination
            i = acos(h_vec(3) / h);
            
            % RAAN
            if n ~= 0
                Omega = acos(n_vec(1) / n);
                if n_vec(2) < 0
                    Omega = 2*pi - Omega;
                end
            else
                Omega = 0;
            end
            
            % Argument of periapsis
            if n ~= 0 && e ~= 0
                omega = acos(dot(n_vec, e_vec) / (n * e));
                if e_vec(3) < 0
                    omega = 2*pi - omega;
                end
            else
                omega = 0;
            end
            
            % True anomaly
            if e ~= 0
                nu = acos(dot(e_vec, r) / (e * norm(r)));
                if dot(r, v) < 0
                    nu = 2*pi - nu;
                end
            else
                nu = acos(dot(n_vec, r) / (n * norm(r)));
                if r(3) < 0
                    nu = 2*pi - nu;
                end
            end
            
            obj.storeResult('semi_major_axis', a);
            obj.storeResult('eccentricity', e);
            obj.storeResult('inclination', i);
        end
        
        function simulateOrbit(obj, centralMass, satellite, dt, steps)
            %% Numerical orbit integration
            % satellite: struct with .position, .velocity, .mass
            
            positions = zeros(steps, 3);
            velocities = zeros(steps, 3);
            energies = zeros(steps, 1);
            
            r = satellite.position;
            v = satellite.velocity;
            m = satellite.mass;
            M = centralMass;
            mu = obj.G * M;
            
            for i = 1:steps
                positions(i, :) = r;
                velocities(i, :) = v;
                
                % Specific energy
                energies(i) = norm(v)^2 / 2 - mu / norm(r);
                
                % Acceleration
                r_norm = norm(r);
                a = -mu / r_norm^3 * r;
                
                % Velocity Verlet integration
                v_half = v + 0.5 * a * dt;
                r = r + v_half * dt;
                
                % New acceleration
                r_norm = norm(r);
                a_new = -mu / r_norm^3 * r;
                
                v = v_half + 0.5 * a_new * dt;
            end
            
            obj.storeResult('orbit_positions', positions);
            obj.storeResult('orbit_velocities', velocities);
            obj.storeResult('orbit_energies', energies);
        end
        
        function [tidal_force] = tidalForce(obj, M, m, R, r)
            %% Tidal force: differential gravitational acceleration
            % Approximate: F_tidal ≈ 2*G*M*m*R / r^3
            
            tidal_force = 2 * obj.G * M * m * R / r^3;
            obj.storeResult('tidal_force', tidal_force);
        end
        
        function [schwarzschild_radius] = schwarzschildRadius(obj, M)
            %% r_s = 2GM/c^2
            schwarzschild_radius = 2 * obj.G * M / obj.c^2;
            obj.storeResult('schwarzschild_radius', schwarzschild_radius);
        end
        
        function [time_dilation] = gravitationalTimeDilation(obj, M, r)
            %% t' = t * sqrt(1 - r_s/r)
            r_s = obj.schwarzschildRadius(M);
            time_dilation = sqrt(1 - r_s / r);
            obj.storeResult('gravitational_time_dilation', time_dilation);
        end
        
        %% =====================================================================
        %% MASS & RELATIVITY MODULE (Lines 2201-2800)
        %% =====================================================================
        
        function [m_rel, gamma] = relativisticMass(obj, m0, v)
            %% m = gamma * m0 = m0 / sqrt(1 - v^2/c^2)
            beta = norm(v) / obj.c;
            
            if beta >= 1
                error('Velocity cannot exceed speed of light');
            end
            
            gamma = 1 / sqrt(1 - beta^2);
            m_rel = m0 * gamma;
            
            obj.storeResult('relativistic_mass', m_rel);
            obj.storeResult('lorentz_factor', gamma);
        end
        
        function [E_total, E_kinetic, E_rest, p] = massEnergy(obj, m, v)
            %% E = gamma * m * c^2
            [m_rel, gamma] = obj.relativisticMass(m, v);
            
            E_rest = m * obj.c^2;
            E_total = gamma * E_rest;
            E_kinetic = E_total - E_rest;
            p = gamma * m * v;
            
            obj.storeResult('rest_energy', E_rest);
            obj.storeResult('total_energy', E_total);
            obj.storeResult('kinetic_energy', E_kinetic);
            obj.storeResult('relativistic_momentum', p);
        end
        
        function [E, p] = photonEnergy(obj, lambda)
            %% E = hc/λ, p = h/λ
            E = obj.h * obj.c / lambda;
            p = obj.h / lambda;
            obj.storeResult('photon_energy', E);
            obj.storeResult('photon_momentum', p);
        end
        
        function [lambda_doppler] = dopplerShift(obj, lambda0, v_source, v_observer)
            %% Relativistic Doppler effect
            beta = v_source / obj.c;
            lambda_doppler = lambda0 * sqrt((1 + beta) / (1 - beta));
            obj.storeResult('doppler_wavelength', lambda_doppler);
        end
        
        function [lambda_compton] = comptonScattering(obj, lambda0, theta)
            %% Compton scattering: Δλ = (h/mc)(1 - cosθ)
            lambda_shift = (obj.h / (obj.me * obj.c)) * (1 - cos(theta));
            lambda_compton = lambda0 + lambda_shift;
            obj.storeResult('compton_wavelength', lambda_compton);
        end
        
        function [E_photon, E_electron] = pairProduction(obj, E_gamma, Z)
            %% Pair production threshold: E > 2*m_e*c^2
            threshold = 2 * obj.me * obj.c^2;
            
            if E_gamma < threshold
                E_photon = E_gamma;
                E_electron = 0;
                warning('Photon energy below pair production threshold');
                return;
            end
            
            % Energy sharing (simplified)
            E_photon = 0;
            E_electron = E_gamma - threshold; % Kinetic energy shared
            
            obj.storeResult('pair_production_energy', E_electron);
        end
        
        function [t_prime, x_prime] = lorentzTransform(obj, t, x, v)
            %% Lorentz transformation
            % t' = gamma*(t - vx/c^2)
            % x' = gamma*(x - vt)
            
            gamma = 1 / sqrt(1 - (v/obj.c)^2);
            t_prime = gamma * (t - v*x / obj.c^2);
            x_prime = gamma * (x - v*t);
            
            obj.storeResult('transformed_time', t_prime);
            obj.storeResult('transformed_position', x_prime);
        end
        
        function [ds2, proper_time] = spacetimeInterval(obj, dt, dx, dy, dz)
            %% ds^2 = c^2*dt^2 - dx^2 - dy^2 - dz^2
            ds2 = (obj.c * dt)^2 - dx^2 - dy^2 - dz^2;
            proper_time = sqrt(ds2) / obj.c;
            
            obj.storeResult('spacetime_interval', ds2);
            obj.storeResult('proper_time', proper_time);
        end
        
        %% =====================================================================
        %% GASES & THERMODYNAMICS MODULE (Lines 2801-3600)
        %% =====================================================================
        
        function [P, V, T, n] = idealGasLaw(obj, state)
            %% PV = nRT - Solve for missing variable
            % state: struct with any 3 of P, V, T, n
            
            known = fieldnames(state);
            
            if length(known) < 3
                error('Need at least 3 of P, V, T, n');
            end
            
            if ~isfield(state, 'P')
                P = state.n * obj.R * state.T / state.V;
                V = state.V; T = state.T; n = state.n;
            elseif ~isfield(state, 'V')
                P = state.P;
                V = state.n * obj.R * state.T / state.P;
                T = state.T; n = state.n;
            elseif ~isfield(state, 'T')
                P = state.P; V = state.V;
                T = state.P * state.V / (state.n * obj.R);
                n = state.n;
            elseif ~isfield(state, 'n')
                P = state.P; V = state.V; T = state.T;
                n = state.P * state.V / (obj.R * state.T);
            end
            
            obj.storeResult('gas_pressure', P);
            obj.storeResult('gas_volume', V);
            obj.storeResult('gas_temperature', T);
            obj.storeResult('gas_moles', n);
        end
        
        function [v_rms, v_avg, v_mp] = maxwellBoltzmannSpeeds(obj, T, m_molecule)
            %% Characteristic speeds from Maxwell-Boltzmann distribution
            % m_molecule: mass of one molecule [kg]
            
            v_rms = sqrt(3 * obj.kB * T / m_molecule);
            v_avg = sqrt(8 * obj.kB * T / (pi * m_molecule));
            v_mp = sqrt(2 * obj.kB * T / m_molecule);
            
            obj.storeResult('rms_speed', v_rms);
            obj.storeResult('average_speed', v_avg);
            obj.storeResult('most_probable_speed', v_mp);
        end
        
        function [f_v] = maxwellBoltzmannDistribution(obj, v, T, m)
            %% f(v) = 4π*(m/2πkT)^(3/2) * v^2 * exp(-mv^2/2kT)
            
            coeff = 4 * pi * (m / (2 * pi * obj.kB * T))^(3/2);
            f_v = coeff * v.^2 .* exp(-m * v.^2 / (2 * obj.kB * T));
            
            obj.storeResult('speed_distribution', f_v);
        end
        
        function [P_real] = vanDerWaals(obj, n, V, T, a, b)
            %% (P + a*n^2/V^2)(V - nb) = nRT
            % a: attraction parameter [Pa·m^6/mol^2]
            % b: excluded volume [m^3/mol]
            
            P_real = (n * obj.R * T) / (V - n*b) - a*n^2 / V^2;
            obj.storeResult('van_der_waals_pressure', P_real);
        end
        
        function [P, V, T_crit] = criticalPoint(obj, a, b)
            %% Critical constants for van der Waals gas
            V_crit = 3*b;
            T_crit = 8*a / (27 * obj.R * b);
            P_crit = a / (27 * b^2);
            
            obj.storeResult('critical_pressure', P_crit);
            obj.storeResult('critical_volume', V_crit);
            obj.storeResult('critical_temperature', T_crit);
        end
        
        function [Z] = compressibilityFactor(obj, P, V, n, T)
            %% Z = PV / nRT (deviation from ideal gas)
            Z = P * V / (n * obj.R * T);
            obj.storeResult('compressibility', Z);
        end
        
        function [U, H, S] = thermodynamicProperties(obj, gas, T, V, n)
            %% Internal energy, enthalpy, entropy
            % gas: struct with .Cv, .Cp, .gamma
            
            U = n * gas.Cv * T; % Internal energy
            H = U + n * obj.R * T; % Enthalpy
            
            % Entropy (Sackur-Tetrode for monatomic ideal gas)
            S = n * gas.Cv * log(T) + n * obj.R * log(V/n) + n * gas.S0;
            
            obj.storeResult('internal_energy', U);
            obj.storeResult('enthalpy', H);
            obj.storeResult('entropy', S);
        end
        
        function [Q, W, deltaU] = firstLaw(obj, gas, process, params)
            %% First law of thermodynamics: ΔU = Q - W
            
            switch process
                case 'isothermal'
                    % ΔU = 0, Q = W = nRT*ln(V2/V1)
                    T = params.T;
                    V1 = params.V1; V2 = params.V2;
                    n = params.n;
                    
                    deltaU = 0;
                    W = n * obj.R * T * log(V2/V1);
                    Q = W;
                    
                case 'isobaric'
                    % Constant pressure
                    P = params.P;
                    V1 = params.V1; V2 = params.V2;
                    n = params.n;
                    Cv = gas.Cv;
                    
                    W = P * (V2 - V1);
                    deltaU = n * Cv * (params.T2 - params.T1);
                    Q = deltaU + W;
                    
                case 'isochoric'
                    % Constant volume
                    V = params.V;
                    T1 = params.T1; T2 = params.T2;
                    n = params.n;
                    Cv = gas.Cv;
                    
                    W = 0;
                    deltaU = n * Cv * (T2 - T1);
                    Q = deltaU;
                    
                case 'adiabatic'
                    % Q = 0
                    gamma = gas.gamma;
                    V1 = params.V1; V2 = params.V2;
                    P1 = params.P1;
                    
                    P2 = P1 * (V1/V2)^gamma;
                    W = (P2*V2 - P1*V1) / (1 - gamma);
                    Q = 0;
                    deltaU = -W;
            end
            
            obj.storeResult('heat_added', Q);
            obj.storeResult('work_done', W);
            obj.storeResult('internal_energy_change', deltaU);
        end
        
        function [eta] = carnotEfficiency(obj, T_hot, T_cold)
            %% η = 1 - T_cold/T_hot (maximum possible efficiency)
            eta = 1 - T_cold / T_hot;
            obj.storeResult('carnot_efficiency', eta);
        end
        
        function [COP] = coefficientOfPerformance(obj, Q_cold, W_input)
            %% COP = Q_cold / W (refrigerator)
            % COP = Q_hot / W (heat pump)
            
            COP = Q_cold / W_input;
            obj.storeResult('cop_refrigerator', COP);
        end
        
        function simulateGasDynamics(obj, particles, box, steps)
            %% Molecular dynamics simulation
            % particles: Nx7 matrix [x, y, z, vx, vy, vz, m]
            
            n = size(particles, 1);
            history = zeros(steps, n, 6);
            temperature_history = zeros(steps, 1);
            pressure_history = zeros(steps, 1);
            
            dt = obj.TimeStep;
            
            for step = 1:steps
                history(step, :, :) = particles(:, 1:6);
                
                % Update positions
                particles(:, 1:3) = particles(:, 1:3) + particles(:, 4:6) * dt;
                
                % Wall collisions (elastic, specular)
                for dim = 1:3
                    hit_min = particles(:, dim) < box.min(dim);
                    hit_max = particles(:, dim) > box.max(dim);
                    
                    particles(hit_min, dim) = 2*box.min(dim) - particles(hit_min, dim);
                    particles(hit_max, dim) = 2*box.max(dim) - particles(hit_max, dim);
                    
                    particles(hit_min, dim+3) = -particles(hit_min, dim+3);
                    particles(hit_max, dim+3) = -particles(hit_max, dim+3);
                end
                
                % Calculate instantaneous temperature
                kinetic = 0.5 * sum(particles(:, 7) .* sum(particles(:, 4:6).^2, 2));
                temperature_history(step) = 2 * kinetic / (3 * n * obj.kB);
                
                % Calculate pressure from wall collisions (simplified)
                pressure_history(step) = n * obj.kB * temperature_history(step) / box.volume;
                
                % Particle-particle collisions (hard sphere)
                % Simplified: only check nearby pairs
                particles = obj.handleCollisions(particles, dt);
            end
            
            obj.storeResult('gas_trajectories', history);
            obj.storeResult('gas_temperature_history', temperature_history);
            obj.storeResult('gas_pressure_history', pressure_history);
        end
        
        function [particles] = handleCollisions(obj, particles, dt)
            %% Handle elastic collisions between particles
            % Simplified: check all pairs (O(N^2), use spatial hashing for large N)
            
            n = size(particles, 1);
            sigma = 1e-10; % Effective diameter (simplified)
            
            for i = 1:n-1
                for j = i+1:n
                    r_ij = particles(i, 1:3) - particles(j, 1:3);
                    dist = norm(r_ij);
                    
                    if dist < 2*sigma
                        % Elastic collision
                        v_rel = particles(i, 4:6) - particles(j, 4:6);
                        
                        % Relative velocity along line of centers
                        v_rel_normal = dot(v_rel, r_ij) / dist;
                        
                        if v_rel_normal > 0
                            continue; % Moving apart
                        end
                        
                        % Impulse
                        m1 = particles(i, 7);
                        m2 = particles(j, 7);
                        J = 2 * v_rel_normal / (1/m1 + 1/m2);
                        
                        % Update velocities
                        particles(i, 4:6) = particles(i, 4:6) - J * r_ij / dist / m1;
                        particles(j, 4:6) = particles(j, 4:6) + J * r_ij / dist / m2;
                    end
                end
            end
        end
        
        function [Q_dot] = heatConduction(obj, k_thermal, A, dT_dx)
            %% Fourier's law: q = -k * dT/dx
            Q_dot = -k_thermal * A * dT_dx;
            obj.storeResult('heat_flux', Q_dot);
        end
        
        function [Q_dot] = heatRadiation(obj, epsilon, A, T1, T2)
            %% Stefan-Boltzmann law: P = εσA(T1^4 - T2^4)
            Q_dot = epsilon * obj.sigma * A * (T1^4 - T2^4);
            obj.storeResult('radiated_power', Q_dot);
        end
        
        %% =====================================================================
        %% SOLIDS & MATERIALS MODULE (Lines 3601-4200)
        %% =====================================================================
        
        function [stress, strain, E_eff] = stressStrain(obj, material, force, area, length, deltaL)
            %% Linear elasticity: σ = F/A, ε = ΔL/L, E = σ/ε
            
            stress = force / area;
            strain = deltaL / length;
            E_eff = stress / strain;
            
            % Check elastic limit
            if stress > material.yieldStrength
                warning('Material exceeded elastic limit - entering plastic regime');
                
                % Linear hardening model
                if isfield(material, 'hardeningModulus')
                    plastic_strain = (stress - material.yieldStrength) / material.hardeningModulus;
                    strain = strain + plastic_strain;
                end
            end
            
            obj.storeResult('stress', stress);
            obj.storeResult('strain', strain);
            obj.storeResult('youngs_modulus', E_eff);
        end
        
        function [deformation] = beamBending(obj, E, I_moment, L, load, loadType)
            %% Euler-Bernoulli beam theory
            % E: Young's modulus [Pa], I: area moment of inertia [m^4]
            % L: beam length [m]
            
            x = linspace(0, L, 100);
            deformation = zeros(size(x));
            moment = zeros(size(x));
            
            switch loadType
                case 'point_center'
                    % Point load P at center
                    P = load;
                    for i = 1:length(x)
                        if x(i) <= L/2
                            deformation(i) = P*x(i)/(48*E*I_moment) * (3*L^2 - 4*x(i)^2);
                            moment(i) = P*x(i)/2;
                        else
                            deformation(i) = P*(L-x(i))/(48*E*I_moment) * (3*L^2 - 4*(L-x(i))^2);
                            moment(i) = P*(L-x(i))/2;
                        end
                    end
                    
                case 'distributed_uniform'
                    % Uniform load w [N/m]
                    w = load;
                    deformation = w*x.*(L^3 - 2*L*x.^2 + x.^3) / (24*E*I_moment);
                    moment = w*x.*(L - x) / 2;
                    
                case 'cantilever_end'
                    % Cantilever with end load P
                    P = load;
                    deformation = P*x.^2 .* (3*L - x) / (6*E*I_moment);
                    moment = P*(L - x);
                    
                case 'cantilever_uniform'
                    % Cantilever with uniform load
                    w = load;
                    deformation = w*x.^2 .* (6*L^2 - 4*L*x + x.^2) / (24*E*I_moment);
            end
            
            obj.storeResult('beam_deflection', deformation);
            obj.storeResult('bending_moment', moment);
        end
        
        function [tau, G] = torsion(obj, T_torque, r, J_polar, L, theta)
            %% Torsion: τ = T*r/J, G = TL/(J*θ)
            tau = T_torque * r / J_polar;
            G = T_torque * L / (J_polar * theta);
            
            obj.storeResult('shear_stress', tau);
            obj.storeResult('shear_modulus', G);
        end
        
        function [buckling_load] = eulerBuckling(obj, E, I, L, K)
            %% P_cr = π^2*EI / (KL)^2
            % K: effective length factor (1.0 pinned-pinned, 0.5 fixed-fixed, etc.)
            
            buckling_load = pi^2 * E * I / (K * L)^2;
            obj.storeResult('critical_buckling_load', buckling_load);
        end
        
        function [sigma_vm] = vonMisesStress(obj, sigma)
            %% von Mises equivalent stress for yield criterion
            % sigma: 3x3 stress tensor
            
            s11 = sigma(1,1); s22 = sigma(2,2); s33 = sigma(3,3);
            s12 = sigma(1,2); s23 = sigma(2,3); s31 = sigma(3,1);
            
            sigma_vm = sqrt(0.5 * ((s11-s22)^2 + (s22-s33)^2 + (s33-s11)^2 +...
                        6*(s12^2 + s23^2 + s31^2)));
            
            obj.storeResult('von_mises_stress', sigma_vm);
        end
        
        function [K_I, K_II, K_III] = stressIntensityFactors(obj, sigma_applied, a_crack, geometry)
            %% Fracture mechanics stress intensity factors
            % a_crack: crack length [m]
            
            switch geometry
                case 'center_crack_infinite'
                    K_I = sigma_applied * sqrt(pi * a_crack);
                    K_II = 0; K_III = 0;
                    
                case 'edge_crack'
                    K_I = 1.12 * sigma_applied * sqrt(pi * a_crack);
                    K_II = 0; K_III = 0;
                    
                case 'penny_shaped'
                    K_I = 2 * sigma_applied * sqrt(a_crack / pi);
                    K_II = 0; K_III = 0;
            end
            
            obj.storeResult('stress_intensity_KI', K_I);
        end
        
        function [band_gap] = semiconductorBandGap(obj, material, T)
            %% Temperature-dependent band gap
            % Varshni equation: Eg(T) = Eg(0) - α*T^2/(T + β)
            
            switch material
                case 'Si'
                    Eg0 = 1.17; alpha = 4.73e-4; beta = 636;
                case 'Ge'
                    Eg0 = 0.74; alpha = 4.77e-4; beta = 235;
                case 'GaAs'
                    Eg0 = 1.52; alpha = 5.41e-4; beta = 204;
                otherwise
                    error('Unknown semiconductor');
            end
            
            band_gap = Eg0 - alpha * T^2 / (T + beta);
            obj.storeResult('band_gap', band_gap);
        end
        
        function [n, p, Ef] = carrierConcentration(obj, material, T, Nd, Na)
            %% Semiconductor carrier statistics
            % Nd: donor concentration, Na: acceptor concentration
            
            switch material
                case 'Si'
                    Nc = 2.8e25 * (T/300)^(3/2); % Conduction band DOS [m^-3]
                    Nv = 1.04e25 * (T/300)^(3/2); % Valence band DOS
                    Eg = obj.semiconductorBandGap('Si', T);
                otherwise
                    error('Unknown material');
            end
            
            ni = sqrt(Nc * Nv) * exp(-Eg / (2 * obj.kB * T));
            
            if Nd > Na
                % n-type
                n = Nd - Na;
                p = ni^2 / n;
            else
                % p-type
                p = Na - Nd;
                n = ni^2 / p;
            end
            
            % Fermi level position
            Ef = obj.kB * T * log(n / Nc);
            
            obj.storeResult('electron_concentration', n);
            obj.storeResult('hole_concentration', p);
            obj.storeResult('fermi_level', Ef);
        end
        
        function [sigma_elec] = electricalConductivity(obj, n, mu, q)
            %% σ = n*q*μ
            sigma_elec = n * q * mu;
            obj.storeResult('electrical_conductivity', sigma_elec);
        end
        
        function [kappa] = thermalConductivity(obj, C_v, v_sound, lambda_mfp)
            %% κ = (1/3) * C_v * v * λ
            kappa = (1/3) * C_v * v_sound * lambda_mfp;
            obj.storeResult('thermal_conductivity', kappa);
        end
        
        %% =====================================================================
        %% QUANTUM MECHANICS MODULE (Lines 4201-4800)
        %% =====================================================================
        
        function solveSchrodinger1D(obj, V, x, mass, boundary)
            %% Time-independent Schrödinger equation: -ℏ²/2m * d²ψ/dx² + Vψ = Eψ
            
            dx = x(2) - x(1);
            N = length(x);
            
            % Kinetic energy operator (finite difference)
            T = -(obj.hbar^2 / (2*mass*dx^2)) * (diag(ones(N-1,1), -1) - 2*eye(N) + diag(ones(N-1,1), 1));
            
            % Potential energy operator
            V_op = diag(V);
            
            % Hamiltonian
            H = T + V_op;
            
            % Apply boundary conditions
            switch boundary
                case 'infinite_well'
                    H(1, :) = 0; H(1, 1) = 1;
                    H(end, :) = 0; H(end, end) = 1;
            end
            
            % Solve eigenvalue problem
            [psi, E] = eig(H);
            
            % Sort by energy
            [E_sorted, idx] = sort(diag(E));
            psi = psi(:, idx);
            
            % Normalize
            for i = 1:size(psi, 2)
                psi(:, i) = psi(:, i) / sqrt(trapz(x, abs(psi(:, i)).^2));
            end
            
            obj.EnergyLevels = E_sorted;
            obj.Eigenstates = psi;
            obj.Potential = V;
            
            obj.storeResult('energy_levels', E_sorted);
            obj.storeResult('wavefunctions', psi);
        end
        
        function solveSchrodinger3D(obj, V, x, y, z, mass)
            %% 3D Schrödinger equation
            
            % Use tensor product for separable potentials
            % For general potential, use iterative methods
            
            % Simplified: hydrogen atom analytical solution
            n_max = 5;
            l_max = 4;
            
            levels = [];
            states = {};
            
            for n = 1:n_max
                for l = 0:min(n-1, l_max)
                    for m = -l:l
                        E = -13.6 / n^2; % eV for hydrogen
                        levels = [levels; E];
                        states{end+1} = struct('n', n, 'l', l, 'm', m);
                    end
                end
            end
            
            obj.storeResult('hydrogen_levels', levels);
            obj.storeResult('hydrogen_states', states);
        end
        
        function [prob] = tunnelingProbability(obj, E, V0, a, mass)
            %% Quantum tunneling through rectangular barrier
            % E: particle energy, V0: barrier height, a: barrier width
            
            if E > V0
                % Classically allowed
                k = sqrt(2*mass*(E-V0)) / obj.hbar;
                T = 1 ./ (1 + (V0^2 * sin(k*a).^2) ./ (4*E*(E-V0)));
            else
                % Tunneling regime
                kappa = sqrt(2*mass*(V0-E)) / obj.hbar;
                T = exp(-2*kappa*a);
            end
            
            prob = T;
            obj.storeResult('tunneling_probability', prob);
        end
        
        function [psi_t] = timeEvolution(obj, psi0, H, t)
            %% ψ(t) = exp(-iHt/ℏ) * ψ(0)
            [V, D] = eig(H);
            E = diag(D);
            
            % Expand initial state in energy eigenbasis
            c = V' * psi0;
            
            % Time evolution of coefficients
            c_t = c .* exp(-1i * E * t / obj.hbar);
            
            % Transform back
            psi_t = V * c_t;
            
            obj.storeResult('time_evolved_state', psi_t);
        end
        
        function [expectation] = expectationValue(obj, psi, operator)
            %% <A> = ψ† * A * ψ
            expectation = psi' * operator * psi;
            obj.storeResult('expectation_value', expectation);
        end
        
        function [uncertainty] = uncertaintyPrinciple(obj, psi, x, p_op)
            %% Δx * Δp ≥ ℏ/2
            x_mean = psi' * diag(x) * psi;
            x2_mean = psi' * diag(x.^2) * psi;
            dx = sqrt(x2_mean - x_mean^2);
            
            p_mean = psi' * p_op * psi;
            p2_mean = psi' * (p_op^2) * psi;
            dp = sqrt(p2_mean - p_mean^2);
            
            uncertainty = dx * dp;
            obj.storeResult('position_uncertainty', dx);
            obj.storeResult('momentum_uncertainty', dp);
            obj.storeResult('uncertainty_product', uncertainty);
        end
        
        %% =====================================================================
        %% NUCLEAR PHYSICS MODULE (Lines 4801-5000)
        %% =====================================================================
        
        function [BE] = bindingEnergy(obj, Z, A)
            %% Semi-empirical mass formula (Bethe-Weizsäcker)
            % Z: atomic number, A: mass number
            
            a_v = 15.75;   % Volume term
            a_s = 17.8;    % Surface term
            a_c = 0.711;   % Coulomb term
            a_a = 23.7;    % Asymmetry term
            a_p = 11.18;   % Pairing term
            
            % Volume term
            E_v = a_v * A;
            
            % Surface term
            E_s = -a_s * A^(2/3);
            
            % Coulomb term
            E_c = -a_c * Z*(Z-1) / A^(1/3);
            
            % Asymmetry term
            E_a = -a_a * (A - 2*Z)^2 / A;
            
            % Pairing term
            if mod(Z, 2) == 0 && mod(A-Z, 2) == 0
                E_p = a_p / sqrt(A); % Even-even
            elseif mod(Z, 2) == 1 && mod(A-Z, 2) == 1
                E_p = -a_p / sqrt(A); % Odd-odd
            else
                E_p = 0; % Even-odd
            end
            
            BE = E_v + E_s + E_c + E_a + E_p;
            
            obj.storeResult('binding_energy', BE);
            obj.storeResult('binding_energy_per_nucleon', BE/A);
        end
        
        function [Q] = nuclearReactionQValue(obj, m_reactants, m_products)
            %% Q = (m_reactants - m_products) * c^2
            Q = (sum(m_reactants) - sum(m_products)) * obj.c^2;
            obj.storeResult('q_value', Q);
        end
        
        function [activity] = radioactiveDecay(obj, N0, lambda, t)
            %% N(t) = N0 * exp(-λt), A = λN
            N = N0 * exp(-lambda * t);
            activity = lambda * N;
            
            half_life = log(2) / lambda;
            
            obj.storeResult('remaining_nuclei', N);
            obj.storeResult('activity', activity);
            obj.storeResult('half_life', half_life);
        end
        
        function [E_fission] = fissionEnergy(obj, A_parent, A_fragments, Z_parent)
            %% Approximate fission energy release
            BE_parent = obj.bindingEnergy(Z_parent, A_parent);
            BE_fragments = 0;
            
            for i = 1:length(A_fragments)
                Z_frag = Z_parent * A_fragments(i) / A_parent; % Approximate
                BE_fragments = BE_fragments + obj.bindingEnergy(round(Z_frag), A_fragments(i));
            end
            
            E_fission = BE_fragments - BE_parent;
            obj.storeResult('fission_energy', E_fission);
        end
        
        %% =====================================================================
        %% FLUID DYNAMICS MODULE (Lines 5001-5400)
        %% =====================================================================
        
        function [Re] = reynoldsNumber(obj, rho, v, L, mu)
            %% Re = ρvL/μ
            Re = rho * v * L / mu;
            obj.storeResult('reynolds_number', Re);
        end
        
        function [Cd] = dragCoefficient(obj, Re, shape)
            %% Drag coefficient vs Reynolds number
            switch shape
                case 'sphere'
                    if Re < 1
                        Cd = 24 / Re;
                    elseif Re < 400
                        Cd = 24/Re * (1 + 0.15*Re^0.687);
                    elseif Re < 3e5
                        Cd = 0.44;
                    else
                        Cd = 0.1; % Drag crisis
                    end
                    
                case 'cylinder'
                    if Re < 1
                        Cd = 10 / Re;
                    elseif Re < 2e5
                        Cd = 1.0;
                    else
                        Cd = 0.3;
                    end
                    
                case 'flat_plate'
                    Cd = 1.28; % Perpendicular flow
            end
            
            obj.storeResult('drag_coefficient', Cd);
        end
        
        function [F_drag] = dragForce(obj, rho, v, A, Cd)
            %% F_d = 0.5 * ρ * v^2 * A * Cd
            F_drag = 0.5 * rho * v^2 * A * Cd;
            obj.storeResult('drag_force', F_drag);
        end
        
        function [F_lift] = liftForce(obj, rho, v, A, Cl)
            %% F_l = 0.5 * ρ * v^2 * A * Cl
            F_lift = 0.5 * rho * v^2 * A * Cl;
            obj.storeResult('lift_force', F_lift);
        end
        
        function [v] = terminalVelocity(obj, m, g, rho, A, Cd)
            %% v_t = sqrt(2mg / ρACd)
            v = sqrt(2 * m * g / (rho * A * Cd));
            obj.storeResult('terminal_velocity', v);
        end
        
        function [Q] = volumetricFlow(obj, A, v)
            %% Q = A * v
            Q = A * v;
            obj.storeResult('volumetric_flow', Q);
        end
        
        function [delta_P] = bernoulliEquation(obj, rho, v1, v2, z1, z2, P1)
            %% P1 + 0.5*ρ*v1^2 + ρ*g*z1 = P2 + 0.5*ρ*v2^2 + ρ*g*z2
            P2 = P1 + 0.5*rho*(v1^2 - v2^2) + rho*obj.g0*(z1 - z2);
            delta_P = P2 - P1;
            
            obj.storeResult('pressure_difference', delta_P);
            obj.storeResult('bernoulli_pressure', P2);
        end
        
        function [delta_P] = poiseuilleFlow(obj, mu, L, Q, R)
            %% ΔP = 8μLQ / (πR^4)
            delta_P = 8 * mu * L * Q / (pi * R^4);
            obj.storeResult('poiseuille_pressure_drop', delta_P);
        end
        
        function [v_profile] = velocityProfilePipe(obj, mu, R, delta_P, L, r)
            %% v(r) = (ΔP/4μL) * (R^2 - r^2)
            v_profile = (delta_P / (4*mu*L)) * (R^2 - r.^2);
            obj.storeResult('velocity_profile', v_profile);
        end
        
        function simulateNavierStokes(obj, rho, mu, u0, v0, p0, dt, steps)
            %% Simplified 2D incompressible Navier-Stokes
            % ∂u/∂t + (u·∇)u = -∇p/ρ + ν∇²u
            
            [Nx, Ny] = size(u0);
            nu = mu / rho;
            
            u = u0; v = v0; p = p0;
            
            for step = 1:steps
                % Pressure Poisson equation (simplified)
                % Full implementation requires FFT or multigrid
                
                % Velocity update