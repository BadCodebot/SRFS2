#include "stdafx.h"
#include "Syndrome.h"
#include "GF16.h"
#include <iostream>
#include <iomanip>

namespace ReedSolomon {

	static const int CODEWORDS_PER_SEGMENT = 8;
	static const int SEGMENT_ALIGNMENT = 64;
	static const int BYTES_PER_CODEWORD = 2;

	Syndrome::Syndrome(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice) :
		_nParityCodewords(nParityCodewords), _nDataCodewords(nDataCodewords),
		_codewordsPerSlice(codewordsPerSlice), _multiplicationTable() {

		size_t totalCodewords = nDataCodewords + nParityCodewords;

		_segmentsPerVector = (_nParityCodewords + CODEWORDS_PER_SEGMENT - 1) / CODEWORDS_PER_SEGMENT;
		_vectors = (uint16_t*)_aligned_malloc(
			_segmentsPerVector * CODEWORDS_PER_SEGMENT * sizeof(uint16_t) * totalCodewords, SEGMENT_ALIGNMENT);


		for (size_t i = 0; i < nParityCodewords; i++) _vectors[i] = 1;

		uint16_t* lastSyndromeVector = nullptr;
		uint16_t* currentSyndromeVector = _vectors;

		for (size_t codewordExponent = 1; codewordExponent < nDataCodewords + nParityCodewords; codewordExponent++) {
			lastSyndromeVector = currentSyndromeVector;
			currentSyndromeVector = lastSyndromeVector + _segmentsPerVector * CODEWORDS_PER_SEGMENT;
			for (size_t i = 0; i < nParityCodewords; i++) currentSyndromeVector[i] = GF16::Multiply(lastSyndromeVector[i], GF16::Exp(i));
		}

		_syndrome = (__m128i*)_aligned_malloc(_segmentsPerVector * CODEWORDS_PER_SEGMENT * sizeof(uint16_t) * _codewordsPerSlice, SEGMENT_ALIGNMENT);
		Reset();
	}


	Syndrome::~Syndrome() {
		_aligned_free(_vectors);
		_aligned_free(_syndrome);
	}

	uint16_t Syndrome::GetSyndrome(size_t codewordOffset, size_t exponent) const {

		size_t segment = exponent / CODEWORDS_PER_SEGMENT;
		size_t segmentOffset = exponent % CODEWORDS_PER_SEGMENT;
		return *((uint16_t*)(_syndrome + segment * _codewordsPerSlice + codewordOffset) + segmentOffset);
	}


	void Syndrome::Reset() {
		memset(_syndrome, 0, BYTES_PER_CODEWORD * CODEWORDS_PER_SEGMENT * _segmentsPerVector * _codewordsPerSlice);
	}

	void Syndrome::AddCodewordSlice(uint16_t* data, size_t exponent) {
		__m128i* vectorSegment = (__m128i*)_vectors + _segmentsPerVector * exponent;
		__m128i* dest = _syndrome;

		for (size_t i = 0; i < _segmentsPerVector; i++, vectorSegment++) {
			_multiplicationTable.Set(*vectorSegment);

			uint16_t p1 = *((uint16_t*)dest);
			uint16_t p2 = *((uint16_t*)(dest + 1));

			_multiplicationTable.MultiplyAndXor(data, dest, _codewordsPerSlice);
			dest += _codewordsPerSlice;
		}
	}

	void Syndrome::GetSyndromeSlice(uint16_t* data, size_t exponent) const {

		size_t segment = exponent / CODEWORDS_PER_SEGMENT;
		size_t segmentOffset = exponent % CODEWORDS_PER_SEGMENT;

		uint16_t* p = (uint16_t*)(_syndrome + _codewordsPerSlice) + segmentOffset;
		uint16_t* dest = (uint16_t*)data;
		for (size_t i = 0; i < _codewordsPerSlice; i++, dest++, p += CODEWORDS_PER_SEGMENT) {
			*dest = *p;
		}
	}

	Syndrome* Syndrome_Construct(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice) {
		return new Syndrome(nDataCodewords, nParityCodewords, codewordsPerSlice);
	}

	void Syndrome_Destruct(Syndrome* p) { delete p; }

	void Syndrome_AddCodewordSlice(Syndrome* p, uint16_t* data, size_t exponent) { p->AddCodewordSlice(data, exponent); }

	void Syndrome_GetSyndromeSlice(const Syndrome* p, uint16_t* data, size_t exponent) { p->GetSyndromeSlice(data, exponent); }
}