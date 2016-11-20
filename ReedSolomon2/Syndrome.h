#pragma once
#include <cstdint>
#include "Generator.h"
#include <immintrin.h>
#include "GF16MultiplicationTable.h"
#include "Parity.h"

namespace ReedSolomon {

	class Syndrome {

	public:

		Syndrome(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice);
		~Syndrome();

		void Reset();

		void AddCodewordSlice(uint16_t* data, size_t exponent);

		void GetSyndromeSlice(uint16_t* data, size_t exponent) const;

		inline size_t GetNParityCodewords() const { return _nParityCodewords; }
		inline size_t GetCodewordsPerSlice() const { return _codewordsPerSlice; }

		uint16_t GetSyndrome(size_t codeword, size_t exponent) const;

	private:

		size_t _nParityCodewords;
		size_t _nDataCodewords;
		size_t _codewordsPerSlice;

		size_t _segmentsPerVector;

		GF16MultiplicationTable _multiplicationTable;

		uint16_t* _vectors;

		__m128i* _syndrome;
	};

	extern "C" {
		__declspec(dllexport) Syndrome* Syndrome_Construct(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice);
		__declspec(dllexport) void Syndrome_Destruct(Syndrome* p);
		__declspec(dllexport) void Syndrome_AddCodewordSlice(Syndrome* p, uint16_t* data, size_t exponent);
		__declspec(dllexport) void Syndrome_GetSyndromeSlice(const Syndrome* p, uint16_t* data, size_t exponent);
	}
}
