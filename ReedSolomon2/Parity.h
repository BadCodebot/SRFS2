#pragma once
#include <cstdint>
#include "Generator.h"
#include <immintrin.h>
#include "GF16MultiplicationTable.h"

namespace ReedSolomon {

	class Parity {

	public:

		Parity(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice);
		~Parity();

		void Reset();

		inline size_t GetNDataCodewords() const { return _nDataCodewords; }
		inline size_t GetNParityCodewords() const { return _nParityCodewords; }
		inline size_t GetNParityBlocks() const { return _parityBlocksPerVector; }
		inline __m128i* GetFirstParityBlock() const { return (__m128i*)_parityVectors; }
		inline size_t GetCodewordsPerSlice() const { return _codewordsPerSlice; }

		void Calculate(uint16_t* data, size_t exponent);
		void GetParity(uint16_t* data, size_t exponent) const;

	private:

		size_t _nParityCodewords;
		size_t _parityBlocksPerVector;
		size_t _nDataCodewords;
		size_t _codewordsPerSlice;

		Generator _generator;
		GF16MultiplicationTable _multiplicationTable;

		uint16_t* _parityVectors;
		__m128i* _parity;
	};

	extern "C" {
		__declspec(dllexport) Parity* Parity_Construct(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice);
		__declspec(dllexport) void Parity_Destruct(Parity* p);
		__declspec(dllexport) void Parity_Reset(Parity* p);
		__declspec(dllexport) void Parity_Calculate(Parity* p, uint16_t* data, size_t codewordIndex);
		__declspec(dllexport) void Parity_GetParity(Parity* p, uint16_t* data, size_t parityIndex);
		__declspec(dllexport) size_t Parity_GetNParityCodewords(Parity* p);
		__declspec(dllexport) size_t Parity_GetNDataCodewords(Parity* p);
		__declspec(dllexport) size_t Parity_GetCodewordsPerSlice(Parity* p);
		__declspec(dllexport) char* Parity_GetFirstParityBlock(Parity* p);
	}
}
