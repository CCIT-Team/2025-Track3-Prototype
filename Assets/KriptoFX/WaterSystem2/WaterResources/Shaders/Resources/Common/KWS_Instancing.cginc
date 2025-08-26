#ifndef KWS_WATER_INSTANCING
#define KWS_WATER_INSTANCING

#if defined(KWS_USE_WATER_INSTANCING)

	#ifndef KWS_WATER_VARIABLES
		#include "..\Common\KWS_WaterVariables.cginc"
	#endif



	inline float GetFlag(uint value, uint bit)
	{
		return (value >> bit) & 0x01;
	}

	inline void UpdateInstanceSeamsAndSkirt(InstancedMeshDataStruct meshData, float2 uvData, inout float4 vertex)
	{
		float quadOffset = uvData.y;
		uint mask = (uint)uvData.x;

		vertex.x -= quadOffset * GetFlag(mask, 1) * meshData.downSeam;
		vertex.z -= quadOffset * GetFlag(mask, 2) * meshData.leftSeam;
		vertex.x += quadOffset * GetFlag(mask, 3) * meshData.topSeam;
		vertex.z += quadOffset * GetFlag(mask, 4) * meshData.rightSeam;
		
		float downSkirt = GetFlag(mask, 5);
		float leftSkirt = GetFlag(mask, 6);
		float topSkirt = GetFlag(mask, 7);
		float rightSkirt = GetFlag(mask, 8);

		vertex.zy += 1000 * downSkirt * meshData.downInf * float2(-1, 0);
		vertex.xy += 1000 * leftSkirt * meshData.leftInf * float2(-1, 0);
		vertex.zy += 1000 * topSkirt * meshData.topInf * float2(1, 0);
		vertex.xy += 1000 * rightSkirt * meshData.rightInf * float2(1, 0);
		
	}


	inline void UpdateInstaceRotation(inout float4 vertex, float4x4 matrixM, float4x4 matrixIM)
	{
		//if (KWS_MeshType == KWS_MESH_TYPE_FINITE_BOX) vertex.xyz = mul((float3x3)KWS_InstancingRotationMatrix, vertex.xyz);

	}

	inline void SetMatrixM(float3 position, float3 size, inout float4x4 matrixM)
	{
		position.y += 0.001;

		matrixM._11_21_31_41 = float4(size.x, 0, 0, 0);
		matrixM._12_22_32_42 = float4(0, size.y, 0, 0);
		matrixM._13_23_33_43 = float4(0, 0, size.z, 0);
		matrixM._14_24_34_44 = float4(position.xyz, 1);
	}

	inline void UpdateInstanceMatrixM(InstancedMeshDataStruct meshData, inout float4x4 matrixM)
	{
		SetMatrixM(meshData.position.xyz, meshData.size.xyz, matrixM);
		matrixM = UpdateCameraRelativeMatrix(matrixM);
	}


	inline void UpdateAllInstanceMatrixes(InstancedMeshDataStruct meshData, inout float4x4 matrixM, inout float4x4 matrixIM)
	{
		SetMatrixM(meshData.position.xyz, meshData.size.xyz, matrixM);
		
		matrixIM = matrixM;
		matrixIM._14_24_34 *= -1;
		matrixIM._11_22_33 = 1.0f / matrixIM._11_22_33;
		
		matrixM = UpdateCameraRelativeMatrix(matrixM);
	}


	inline void UpdateAllInstanceMatrixes(uint instanceID, inout float4x4 matrixM, inout float4x4 matrixIM)
	{
		UpdateAllInstanceMatrixes(InstancedMeshData[instanceID], matrixM, matrixIM);
	}


	inline void UpdateInstanceData(uint instanceID, float2 uvData, inout float4 vertex, inout float4x4 matrixM, inout float4x4 matrixIM)
	{
		InstancedMeshDataStruct meshData = InstancedMeshData[instanceID];
		UpdateInstanceSeamsAndSkirt(meshData, uvData, vertex);
		
		UpdateInstaceRotation(vertex, matrixM, matrixIM);
		UpdateAllInstanceMatrixes(meshData, matrixM, matrixIM);
	}
#endif
#endif